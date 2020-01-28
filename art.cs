using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Art
{
    // ideas:
    //      - semantic color (object association, image-processing tags like edge, and image composition tags (background element, focus)
    //          - "smart filters" or semantic filters can then perform localized, specialized, or complex image processing

    class art
    {
        Bitmap img;
        Grid<ArtColor> canvas;
        protected int SizeCols, SizeRows, colorDepth, gridSize;
        System.Drawing.Imaging.PixelFormat pixelFormat;
        
        public art()
        {
            init();
            //testFill();
            //fishEye();
            //blur();
            HilbertFill();
            saveBMP("Test2");
        }

        private void init()
        {
            gridSize = 32;
            SizeCols = gridSize * 63;
            SizeRows = gridSize * 63;
            colorDepth = 256;
            
            pixelFormat = System.Drawing.Imaging.PixelFormat.Format24bppRgb;
            img = newBMP();
            canvas = new Grid<ArtColor>(SizeRows, SizeCols);
        }

        private Bitmap newBMP(int newSizeHorizontal = 0, int newSizeVertical = 0)
        {
            if (newSizeHorizontal > 0 && newSizeVertical > 0)
            {
                SizeCols = newSizeHorizontal;
                SizeRows = newSizeVertical;
            }
            return new Bitmap(SizeRows, SizeCols, pixelFormat);
        }

        private void CanvasToBMP()
        {
            for (int row = 0; row < SizeRows; row++)
            {
                for (int col = 0; col < SizeCols; col++)
                {
                    img.SetPixel(row, col, canvas.GetCell(row, col).Render());
                }
            }
        }

        private void saveBMP(string name)
        {
            CanvasToBMP();
            img.Save("../../imgOutput/" + name + ".bmp");//".png", System.Drawing.Imaging.ImageFormat.Png);
        }


        protected class HilbertOperation : GridMethods<bool, Tuple<Grid<bool>, art>>
        {
            public HilbertOperation(Tuple<Grid<bool>, art> _context) : base(_context)
            {
            }

            public override bool Map2D(out bool update, int row, int col)
            {
                update = false;
                bool value = context.Item1.GetCell(row, col);
                for (int subRow = 0; subRow < context.Item2.gridSize; subRow++)
                {
                    for (int subCol = 0; subCol < context.Item2.gridSize; subCol++)
                    {
                        context.Item1.SetCell((row * context.Item2.gridSize) + subRow, (col * context.Item2.gridSize) + subCol, value);
                    }
                }
                return true;
            }
        }
        private void HilbertFill()
        {
            var curve = HilbertCurve.GenerateHilbertCurve(HilbertIterationsRequired());
            // inflate curve to gridSize (this could be a grid method eventually)
            var temp = new Grid<bool>(curve.sizeRows * gridSize, curve.sizeCols * gridSize);
            curve.For2D(new HilbertOperation(new Tuple<Grid<bool>, art>(temp, this)));
            //for (int row = 0; row < curve.sizeRows; row++)
            //{
            //    for (int col = 0; col < curve.sizeCols; col++)
            //    {
            //        bool value = curve.GetCell(row, col);
            //        for (int subRow = 0; subRow < gridSize; subRow++)
            //        {
            //            for (int subCol = 0; subCol < gridSize; subCol++)
            //            {
            //                temp.SetCell((row * gridSize) + subRow, (col * gridSize) + subCol, value);
            //            }
            //        }
            //    }
            //}
            curve = temp;

            curve = curve.Crop(0, 0, SizeRows, SizeCols);

            for (int row = 0; row < SizeRows; row++)
            {
                for (int col = 0; col < SizeCols; col++)
                {
                    canvas.SetCell(row, col, funColor2(row, col, curve.GetCell(row, col)));
                    //if (curve.GetCell(row, col) == true)
                    //{
                    //    canvas.SetCell(row, col, new ArtColor(255, 0, 0));
                    //} else
                    //{
                    //    canvas.SetCell(row, col, new ArtColor(0, 0, 0));
                    //}
                }
            }
        }

        private void testFill()
        {
            for (int row = 0; row < SizeRows; row++)
            {
                for (int col = 0; col < SizeCols; col++)
                {
                    img.SetPixel(row, col, CheckerColor(row, col).Render());
                }
            }
        }

        private ArtColor CheckerColor(int x, int y)
        {
            int red, green, blue;
            if ((Math.Floor(x /32.0) % 2) != (Math.Floor(y / 32.0) % 2))
            {
                red = 255;
            } else
            {
                red = 0;
            }
            green = 0;
            blue = 0;
            return new ArtColor(red, green, blue);
        }

        private void blur()
        {
            var buffer = newBMP();
            for (int row = 0; row < SizeRows; row++)
            {
                for (int col = 0; col < SizeRows; col++)
                {
                    var neighbors = new List<Coord>();
                    if (row > 0)
                    {
                        neighbors.Add(new Coord(row - 1, col));
                    }
                    if (row < SizeCols - 1)
                    {
                        neighbors.Add(new Coord(row + 1, col));
                    }
                    if (col > 0)
                    {
                        neighbors.Add(new Coord(row, col - 1));
                    }
                    if (col < SizeRows - 1)
                    {
                        neighbors.Add(new Coord(row, col + 1));
                    }

                    List<Color> neighborColors = new List<Color>();
                    foreach (var n in neighbors)
                    {
                        neighborColors.Add(img.GetPixel(n.x, n.y));
                    }
                    int red = neighborColors.Select(z => (int)z.R).Cast<int>().Sum() / neighborColors.Count();
                    int green = neighborColors.Select(z => (int)z.G).Cast<int>().Sum() / neighborColors.Count();
                    int blue = neighborColors.Select(z => (int)z.B).Sum() / neighborColors.Count();
                    buffer.SetPixel(row, col, new ArtColor(red, green, blue).Render());
                }
            }
            img = buffer;
        }

        private void fishEye()
        {
            var buffer = newBMP();
            for(int row = 0; row < SizeRows; row++)
            {
                for (int col = 0; col < SizeCols; col++)
                {
                    double run = (SizeCols / 2.0) - row;
                    double rise = (SizeRows / 2.0) - col;
                    int fishbowlRow = row + (int)(Math.Floor(run * .05));
                    int fishbowlCol = col + (int)(Math.Floor(rise * .05));
                    if (fishbowlRow < 0)
                    {
                        fishbowlRow = 0;
                    }
                    if (fishbowlRow > SizeCols)
                    {
                        fishbowlRow = SizeCols;
                    }
                    if (fishbowlCol < 0)
                    {
                        fishbowlCol = 0;
                    }
                    if (fishbowlCol > SizeRows)
                    {
                        fishbowlCol = SizeRows;
                    }

                    buffer.SetPixel(row, col, img.GetPixel(fishbowlRow, fishbowlCol));
                }
            }
            img = buffer;
        }

        private Tuple<int, int, int> spinColor(int red, int green, int blue)
        {
            int temp = red;
            red = green;
            green = blue;
            blue = temp;

            return new Tuple<int, int, int>(red, green, blue);
        }

        private ArtColor funColor2(int row, int col, bool isHilbert)
        {
            int red = (int)(Math.Floor((colorDepth / (double)SizeRows) * row));
            int green = (row % gridSize * 8);
            int blue = (col % gridSize * 8);



            if (green % gridSize == 24)
            {
                //green = (green % colorDepth) + (colorDepth - (green % colorDepth) / 2);
            }
            if (blue % gridSize == 24)
            {
                //blue = (blue % colorDepth) + (colorDepth - (blue % colorDepth) / 2);
            }

            if (isHilbert)
            {
                var T = spinColor(red, green, blue);
                //T = spinColor(T.Item1, T.Item2, T.Item3);
                red = T.Item1;
                green = T.Item2;
                blue = T.Item3;
            }
            red %= 256;
            green %= 256;
            blue %= 256;
            green = green.Clamp(100, 200);
            return new ArtColor(red % 256, green % 256, blue % 256);
        }

        private ArtColor funColor(int row, int col)
        {
            int red = (int)(Math.Floor((colorDepth / (double)SizeCols) * row));
            int green = (row * 8);
            int blue = (col * 8);

            

            if (green % 32 == 24)
            {
                green = (green % colorDepth) + (colorDepth - (green % colorDepth) / 2);
            }
            if (blue % 32 == 24)
            {
                blue = (blue % colorDepth) + (colorDepth - (blue % colorDepth) / 2);
            }

            if (testCoord(row, col))
            {
                var T = spinColor(red, green, blue);
                //T = spinColor(T.Item1, T.Item2, T.Item3);
                red = T.Item1;
                green = T.Item2;
                blue = T.Item3;
            }

            return new ArtColor(red, green, blue);
        }

        private int HilbertIterationsRequired()
        {
            // find number of recursions required
            int iterations = 1;
            int size = 1;

            int maxDimension = (int)Math.Floor(Math.Max(SizeCols, SizeRows) / (double)gridSize);
            while (size < maxDimension)
            {
                size *= 4;
                iterations++;
            }
            return iterations;
        }

        
        private void CYMFill()
        {
            Color cyan = Color.FromArgb(0, 255, 255);
            Color yellow = Color.FromArgb(255, 255, 0);
            Color magenta = Color.FromArgb(255, 0, 255);
            int cellSize = 8;
            int quadSize = 8;
            int pixelWidth = 2;
            int pixelHeight = 2;


        }

        private bool testCoord(int row, int col)
        {
            int smallRow = row.FloorDivide(32);
            int smallCol = col.FloorDivide(32);
            if (smallRow % 2 != smallCol % 2)
            {
                return true;
            }
            return false;
        }
    }

    public class Coord
    {
        public int x, y;

        public Coord(int _x, int _y)
        {
            x = _x;
            y = _y;
        }
    }

    public class ArtColor
    {
        private int _red, _green, _blue, colorRange;
        public int red { get => _red; set { _red = value.Clamp(0, colorRange); } }
        public int green { get => _green; set { _green = value.Clamp(0, colorRange); } }
        public int blue { get => _blue; set { _blue = value.Clamp(0, colorRange); } }

        public ArtColor(int r, int g, int b, int _colorRange = 256)
        {
            red = r;
            green = g;
            blue = b;
            colorRange = _colorRange - 1;
        }

        public ArtColor()
        {
            red = 0;
            green = 0;
            blue = 0;
            colorRange = 255;
        }

        public System.Drawing.Color Render()
        {
            return System.Drawing.Color.FromArgb(red, green, blue);
        }
    }

    public abstract class Map2DOld<T> where T : new()
    {
        public enum OrderEnum
        {
            Beginning,
            End,
            Only
        }
        private List<List<T>> resultInProgress;
        private List<T> rowInProgress;
        public virtual List<List<T>> Bookend(List<List<T>> input, OrderEnum order)
        {
            if (order == OrderEnum.Beginning)
            {
                resultInProgress = new List<List<T>>();
            }
            return resultInProgress;
        }
        public virtual List<T> Row(List<T> input, OrderEnum order)
        {
            if (order == OrderEnum.Beginning)
            {
                rowInProgress = new List<T>();
            } else if (order == OrderEnum.End)
            {
                resultInProgress.Add(rowInProgress);
            }
            return rowInProgress;
        }
        public virtual T Cell(T input, OrderEnum order)
        {
            var cell = new T();
            rowInProgress.Add(cell);
            return cell;
        }
    }

    public class Grid<T> where T : new()
    {
        public int sizeCols, sizeRows;
        List<List<T>> grid;
        public Grid(int _sizeRows, int _sizeCols)
        {
            sizeRows = _sizeRows;
            sizeCols = _sizeCols;
            grid = new List<List<T>>();
            for (int row = 0; row < sizeRows; row++)
            {
                var gridRow = new List<T>();
                for (int col = 0; col < sizeCols; col++)
                {
                    gridRow.Add(new T());
                }
                grid.Add(gridRow);
            }
        }

        public bool SetCell(int row, int col, T c)
        {
            if (row >= 0 && row < sizeRows && col >= 0 && col < sizeCols)
            {
                grid[row][col] = c;
                return true;
            }
            return false;
        }

        public T GetCell(int row, int col)
        {
            if (row >= 0 && row < sizeRows && col >= 0 && col < sizeCols)
            {
                return grid[row][col];
            }
            throw new Exception("Coordinates out of range");
        }

        public bool InFrame(int row, int col)
        {
            if (row >= 0 && row < sizeRows && col >= 0 && col < sizeCols)
            {
                return true;
            }
            return false;
        }

        public Grid<T> Crop(int topLeftRow, int topLeftCol, int _sizeRows, int _sizeCols)
        {
            var result = new Grid<T>(_sizeRows, _sizeCols);
            for (int row = 0; row < _sizeRows; row++)
            {
                for (int col = 0; col < _sizeCols; col++)
                {
                    if (!InFrame(topLeftRow + row, topLeftCol + col))
                    {
                        result.SetCell(row, col, new T());
                    } else
                    {
                        result.SetCell(row, col, GetCell((topLeftRow + row), (topLeftCol + col)));
                    }
                }
            }
            return result;
        }

        // to do: "blend" action that's like stamp, but calls a method on T for resolving the interaction
        //      also: revisit this method when adding opacity
        public bool Stamp(int topLeftRow, int topLeftCol, Grid<T> grid)
        {
            for (int row = 0; row < grid.sizeRows; row++)
            {
                for (int col = 0; col < grid.sizeCols; col++)
                {
                    int offsetRow = topLeftRow + row;
                    int offsetCol = topLeftCol + col;
                    if (offsetRow >= 0 && offsetRow < sizeRows
                        && offsetCol >= 0 && offsetCol < sizeCols)
                    {
                        SetCell(offsetRow, offsetCol, grid.GetCell(row, col));
                    }
                }
            }
            return true;
        }

        public void For2D(GridMethods<T> handler)
        {
            for (int row = 0; row < sizeRows; row++)
            {
                for (int col = 0; col < sizeCols; col++)
                {
                    bool update;
                    T cell = handler.Map2D(out update, row, col);
                    if (update)
                    {
                        SetCell(row, col, cell);
                    }
                }
            }
        }

        public List<T> diagnostic()
        {
            var result = new List<T>();
            for (int row = 0; row < sizeRows; row++)
            {
                for (int col = 0; col < sizeCols; col++)
                {
                    result.Add(GetCell(row, col));
                }
            }
            return result;
        }
    }

    // curve is not correct
    public static class HilbertCurve
    {
        public enum HilbertCurveComponent
        {
            A, B, C, D
        }

        private static Grid<bool> ResolveComponent(HilbertCurveComponent val)
        {

            var result = new Grid<bool>(3, 3);
            // corners are always true
            result.SetCell(0, 0, true); // top left
            result.SetCell(0, 2, true); // top right
            result.SetCell(2, 0, true); // bottom left
            result.SetCell(2, 2, true); // bottom right
            if (val == HilbertCurveComponent.A) // bottom missing
            {
                result.SetCell(0, 1, true); // top middle
                result.SetCell(1, 0, true); // left middle
                result.SetCell(1, 2, true); // right middle
            } else if (val == HilbertCurveComponent.B) // right missing
            {
                result.SetCell(0, 1, true); // top middle
                result.SetCell(1, 0, true); // left middle
                result.SetCell(2, 1, true); // bottom middle
            } else if (val == HilbertCurveComponent.C) // top missing
            {
                result.SetCell(1, 0, true); // left middle
                result.SetCell(1, 2, true); // right middle
                result.SetCell(2, 1, true); // bottom middle
            } else if (val == HilbertCurveComponent.D) // left missing
            {
                result.SetCell(0, 1, true); // top middle
                result.SetCell(1, 2, true); // right middle
                result.SetCell(2, 1, true); // bottom middle
            }
            return result;
        }

        private static Grid<HilbertCurveComponent> IterateHilbertCurve(Grid<HilbertCurveComponent> start)
        {
            var finish = new Grid<HilbertCurveComponent>(start.sizeRows * 2, start.sizeCols * 2);
            for (int row = 0; row < start.sizeRows; row++)
            {
                for (int col = 0; col < start.sizeCols; col++)
                {
                    if (start.GetCell(row, col) == HilbertCurveComponent.A)
                    {
                        finish.SetCell((row * 2), (col * 2), HilbertCurveComponent.A); // top left
                        finish.SetCell((row * 2), (col * 2) + 1, HilbertCurveComponent.A); // top right
                        finish.SetCell((row * 2) + 1, (col * 2), HilbertCurveComponent.D); // bottom left
                        finish.SetCell((row * 2) + 1, (col * 2) + 1, HilbertCurveComponent.B); // bottom right
                    } else if (start.GetCell(row, col) == HilbertCurveComponent.B)
                    {
                        finish.SetCell((row * 2), (col * 2), HilbertCurveComponent.B); // top left
                        finish.SetCell((row * 2), (col * 2) + 1, HilbertCurveComponent.C); // top right
                        finish.SetCell((row * 2) + 1, (col * 2), HilbertCurveComponent.B); // bottom left
                        finish.SetCell((row * 2) + 1, (col * 2) + 1, HilbertCurveComponent.A); // bottom right
                    } else if (start.GetCell(row, col) == HilbertCurveComponent.C)
                    {
                        finish.SetCell((row * 2), (col * 2), HilbertCurveComponent.D); // top left
                        finish.SetCell((row * 2), (col * 2) + 1, HilbertCurveComponent.B); // top right
                        finish.SetCell((row * 2) + 1, (col * 2), HilbertCurveComponent.C); // bottom left
                        finish.SetCell((row * 2) + 1, (col * 2) + 1, HilbertCurveComponent.C); // bottom right
                    } else if (start.GetCell(row, col) == HilbertCurveComponent.D)
                    {
                        finish.SetCell((row * 2), (col * 2), HilbertCurveComponent.C); // top left
                        finish.SetCell((row * 2), (col * 2) + 1, HilbertCurveComponent.D); // top right
                        finish.SetCell((row * 2) + 1, (col * 2), HilbertCurveComponent.A); // bottom left
                        finish.SetCell((row * 2) + 1, (col * 2) + 1, HilbertCurveComponent.D); // bottom right
                    }
                }
            }
            return finish;
        }

        private static Grid<HilbertCurveComponent> initialState(HilbertCurveComponent orientation)
        {
            var result = new Grid<HilbertCurveComponent>(1, 1);
            result.SetCell(0, 0, orientation);
            return result;
        }

        private static Grid<bool> ResolveCurve(Grid<HilbertCurveComponent> curve)
        {
            // 1. render components to bools
            var result = new Grid<bool>(curve.sizeRows * 4, curve.sizeCols * 4);
            for (int row = 0; row < curve.sizeRows; row++)
            {
                for (int col = 0; col < curve.sizeCols; col++)
                {
                    result.Stamp(row * 4, col * 4, ResolveComponent(curve.GetCell(row, col)));
                }
            }

            // 2. connect the blocks
            for (int row = 0; row < result.sizeRows; row++)
            {
                for (int col = 0; col < result.sizeCols; col++)
                {
                    if (result.GetCell(row, col) == true && CountNeighbors(result, row, col) == 1)
                    {
                        if (row > 1) // check left connection
                        {
                            if (CountNeighbors(result, row - 2, col) == 1 && curve.GetCell((int)Math.Floor(row / 4.0), (int)Math.Floor(col / 4.0)) != HilbertCurveComponent.D)
                            {
                                result.SetCell(row - 1, col, true);
                            }
                        }
                        if (row < result.sizeRows - 2) // check right connection
                        {
                            if (CountNeighbors(result, row + 2, col) == 1 && curve.GetCell((int)Math.Floor(row / 4.0), (int)Math.Floor(col / 4.0)) != HilbertCurveComponent.B)
                            {
                                result.SetCell(row + 1, col, true);
                            }
                        }
                        if (col > 1) // check up connection
                        {
                            if (CountNeighbors(result, row, col - 2) == 1 && curve.GetCell((int)Math.Floor(row / 4.0), (int)Math.Floor(col / 4.0)) != HilbertCurveComponent.C)
                            {
                                result.SetCell(row, col - 1, true);
                            }
                        }
                        if (col < result.sizeCols - 2) // check down connection
                        {
                            if (CountNeighbors(result, row, col + 2) == 1 && curve.GetCell((int)Math.Floor(row / 4.0), (int)Math.Floor(col / 4.0)) != HilbertCurveComponent.A)
                            {
                                result.SetCell(row, col + 1, true);
                            }
                        }
                    }
                }
            }
            return result;
        }

        private static int CountNeighbors(Grid<bool> grid, int row, int col)
        {
            int count = 0;
            if (row > 0)
            {
                count += grid.GetCell(row - 1, col) == true ? 1 : 0;
            }
            if (row < grid.sizeRows - 1)
            {
                count += grid.GetCell(row + 1, col) == true ? 1 : 0;
            }
            if (col > 0)
            {
                count += grid.GetCell(row, col - 1) == true ? 1 : 0;
            }
            if (col < grid.sizeCols - 1)
            {
                count += grid.GetCell(row, col + 1) == true ? 1 : 0;
            }
            return count;
        }

        public static Grid<bool> GenerateHilbertCurve(int size, HilbertCurveComponent orientation = HilbertCurveComponent.A)
        {
            var result = initialState(orientation);
            for (int x = 0; x < size; x++)
            {
                result = IterateHilbertCurve(result);
            }
            return ResolveCurve(result);
        }
    }

    public static class Methods
    {
        public static int Clamp(this int val, int min, int max)
        {
            return Math.Min(Math.Max(val, min), max);
        }

        public static int FloorDivide(this int numerator, int denominator)
        {
            return (int)(Math.Floor(numerator / (double)denominator));
        }

        public static int FloorDivide(this int numerator, double denominator)
        {
            return (int)(Math.Floor(numerator / denominator));
        }

        public static int FloorDivide(this double numerator, int denominator)
        {
            return (int)(Math.Floor(numerator / denominator));
        }

        public static int FloorDivide(this double numerator, double denominator)
        {
            return (int)(Math.Floor(numerator / denominator));
        }

        
    }

    public class GridMethods<T, Context> where T : new()
    {
        protected Context context;
        public GridMethods(Context _context)
        {
            context = _context;
        }
        public virtual T Map2D(out bool update, int row, int col)
        {
            update = true;
            return new T();
        }
    }
}
