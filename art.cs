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
            saveBMP("Test3");
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


        private void HilbertFill()
        {
            var curve = HilbertCurve.GenerateHilbertCurve(HilbertIterationsRequired());
            // inflate curve to gridSize (this could be a grid method eventually)
            var temp = new Grid<bool>(curve.sizeRows * gridSize, curve.sizeCols * gridSize);
            foreach (Cell<bool> C in curve.EachCell())
            {
                bool value = curve.GetCell(C.row, C.col);
                foreach (var C2 in Methods.EachPoint(gridSize, gridSize))
                {
                    temp.SetCell((C.row * gridSize) + C2.row, (C.col * gridSize) + C2.col, value);
                }
            }
            curve = temp;

            curve = curve.Crop(0, 0, SizeRows, SizeCols);

            foreach (var cell in curve.EachCell())
            {
                canvas.SetCell(cell.row, cell.col, funColor2(cell.row, cell.col, curve.GetCell(cell.row, cell.col)));
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
                        neighborColors.Add(img.GetPixel(n.row, n.col));
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
        public int row, col;

        public Coord(int _row, int _col)
        {
            row = _row;
            col = _col;
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

    public class Cell<T> where T : new()
    {
        private int _row, _col;
        public int row { get { return _row; } set { } }
        public int col { get { return _col; } set { } }
        public T value;
        public Cell(int r, int c)
        {
            _row = r;
            _col = c;
            value = new T();
        }
    }

    public class Grid<T> where T : new()
    {
        public int sizeCols, sizeRows;
        List<List<Cell<T>>> grid;
        public Grid(int _sizeRows, int _sizeCols)
        {
            sizeRows = _sizeRows;
            sizeCols = _sizeCols;
            grid = new List<List<Cell<T>>>();
            for (int row = 0; row < sizeRows; row++)
            {
                var gridRow = new List<Cell<T>>();
                for (int col = 0; col < sizeCols; col++)
                {
                    gridRow.Add(new Cell<T>(row, col));
                }
                grid.Add(gridRow);
            }
        }

        public bool SetCell(int row, int col, T value)
        {
            if (row >= 0 && row < sizeRows && col >= 0 && col < sizeCols)
            {
                grid[row][col].value = value;
                return true;
            }
            return false;
        }

        public T GetCell(int row, int col)
        {
            if (row >= 0 && row < sizeRows && col >= 0 && col < sizeCols)
            {
                return grid[row][col].value;
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
            foreach (var C in Methods.EachPoint(_sizeRows, _sizeCols))
            {
                if (!InFrame(topLeftRow + C.row, topLeftCol + C.col))
                {
                    result.SetCell(C.row, C.col, new T());
                }
                else
                {
                    result.SetCell(C.row, C.col, GetCell((topLeftRow + C.row), (topLeftCol + C.col)));
                }
            }     
            return result;
        }

        public List<Cell<T>> EachCell()
        {
            return grid.SelectMany(x => x).ToList();
        }

        // to do: "blend" action that's like stamp, but calls a method on T for resolving the interaction
        //      also: revisit this method when adding opacity
        public bool Stamp(int topLeftRow, int topLeftCol, Grid<T> grid)
        {
            foreach (var C in Methods.EachPoint(grid.sizeRows, grid.sizeCols))
            {
                int offsetRow = topLeftRow + C.row;
                int offsetCol = topLeftCol + C.col;
                if (offsetRow >= 0 && offsetRow < sizeRows
                    && offsetCol >= 0 && offsetCol < sizeCols)
                {
                    SetCell(offsetRow, offsetCol, grid.GetCell(C.row, C.col));
                }
            }
            return true;
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

        private static Grid<HilbertCurveComponent> IterateCurveComponent(HilbertCurveComponent component)
        {
            List<HilbertCurveComponent> list;
            switch (component)
            {
                case HilbertCurveComponent.A:
                    list = new List<HilbertCurveComponent> { HilbertCurveComponent.A, HilbertCurveComponent.A, 
                                                             HilbertCurveComponent.D, HilbertCurveComponent.B };
                    break;
                case HilbertCurveComponent.B:
                    list = new List<HilbertCurveComponent> { HilbertCurveComponent.B, HilbertCurveComponent.C, 
                                                             HilbertCurveComponent.B, HilbertCurveComponent.A };
                    break;
                case HilbertCurveComponent.C:
                    list = new List<HilbertCurveComponent> { HilbertCurveComponent.D, HilbertCurveComponent.B, 
                                                             HilbertCurveComponent.C, HilbertCurveComponent.C };
                    break;
                case HilbertCurveComponent.D:
                    list = new List<HilbertCurveComponent> { HilbertCurveComponent.C, HilbertCurveComponent.D, 
                                                             HilbertCurveComponent.A, HilbertCurveComponent.D };
                    break;
                default:
                    throw new Exception("Hilbert Curve component " + component.ToString() + " not handled in IterateCurveComponent()");
            }
            return list.ToGrid(2, 2);
        }

        private static Grid<HilbertCurveComponent> IterateHilbertCurve(Grid<HilbertCurveComponent> start)
        {
            var finish = new Grid<HilbertCurveComponent>(start.sizeRows * 2, start.sizeCols * 2);
            foreach (var C in start.EachCell())
            {
                finish.Stamp(C.row * 2, C.col * 2, IterateCurveComponent(C.value));
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
            foreach (var C in curve.EachCell())
            {
                result.Stamp(C.row * 4, C.col * 4, ResolveComponent(C.value));
            }

            // 2. connect the blocks
            foreach (var C in result.EachCell())
            {
                if (C.value == true && CountNeighbors(result, C.row, C.col) == 1)
                {
                    if (C.row > 1) // check left connection
                    {
                        if (CountNeighbors(result, C.row - 2, C.col) == 1 && curve.GetCell(C.row.FloorDivide(4), C.col.FloorDivide(4)) != HilbertCurveComponent.D)
                        {
                            result.SetCell(C.row - 1, C.col, true);
                        }
                    }
                    if (C.row < result.sizeRows - 2) // check right connection
                    {
                        if (CountNeighbors(result, C.row + 2, C.col) == 1 && curve.GetCell(C.row.FloorDivide(4), C.col.FloorDivide(4)) != HilbertCurveComponent.B)
                        {
                            result.SetCell(C.row + 1, C.col, true);
                        }
                    }
                    if (C.col > 1) // check up connection
                    {
                        if (CountNeighbors(result, C.row, C.col - 2) == 1 && curve.GetCell(C.row.FloorDivide(4), C.col.FloorDivide(4)) != HilbertCurveComponent.C)
                        {
                            result.SetCell(C.row, C.col - 1, true);
                        }
                    }
                    if (C.col < result.sizeCols - 2) // check down connection
                    {
                        if (CountNeighbors(result, C.row, C.col + 2) == 1 && curve.GetCell(C.row.FloorDivide(4), C.col.FloorDivide(4)) != HilbertCurveComponent.A)
                        {
                            result.SetCell(C.row, C.col + 1, true);
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

        public static List<Coord> EachPoint(int rows, int cols)
        {
            var result = new List<Coord>();
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    result.Add(new Coord(row, col));
                }
            }
            return result;
        }

        public static List<List<T>> InitializeRect<T>(this List<List<T>> rect, int rows, int cols) where T : new()
        {
            var result = new List<List<T>>();
            for (int row = 0; row < rows; row++)
            {
                var r = new List<T>();
                for (int col = 0; col < cols; col++)
                {
                    r.Add(new T());
                }
                result.Add(r);
            }
            return result;
        }

        public static Grid<T> ToGrid<T>(this List<T> list, int rows, int cols) where T : new()
        {
            var grid = new Grid<T>(rows, cols);
            foreach(var C in grid.EachCell())
            {
                C.value = list[(C.row * cols) + C.col];
            }
            return grid;
        }
    }
}
