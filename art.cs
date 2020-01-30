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
        protected Coord size;
        protected int colorDepth, gridSize;
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
            size = new Coord(gridSize * 63, gridSize * 63);
            colorDepth = 256;
            
            pixelFormat = System.Drawing.Imaging.PixelFormat.Format24bppRgb;
            img = newBMP();
            canvas = new Grid<ArtColor>(size);
        }

        private Bitmap newBMP(Coord arg_size = null)
        {
            if (arg_size == null)
            {
                arg_size = size;
            }
            return new Bitmap(arg_size.row, arg_size.col, pixelFormat);
        }

        private void CanvasToBMP()
        {
            foreach (var C in canvas.EachCell())
            {
                img.SetPixel(C.loc.row, C.loc.col, C.value.Render());
            }
        }

        private void saveBMP(string name)
        {
            CanvasToBMP();
            img.Save("../../ImgOutput/" + name + ".bmp");//".png", System.Drawing.Imaging.ImageFormat.Png);
        }


        private void HilbertFill()
        {
            var curve = HilbertCurve.GenerateHilbertCurve(HilbertIterationsRequired());
            // inflate curve to gridSize (this could be a grid method eventually)
            var temp = new Grid<bool>(curve.gridSize.Times(gridSize));
            foreach (Cell<bool> C in curve.EachCell())
            {
                bool value = curve.GetCell(C.loc);
                foreach (var C2 in Methods.EachPoint(new Coord(gridSize)))
                {
                    temp.SetCell(C.loc.Times(gridSize).Plus(C2), value);
                }
            }
            curve = temp;

            curve = curve.Crop(new Coord(0,0), size);

            foreach (var cell in curve.EachCell())
            {
                canvas.SetCell(cell.loc, funColor2(cell.loc, cell.value));
            }
        }

        private void testFill()
        {
            foreach(var C in size.EachPoint())
            {
                canvas.SetCell(C, CheckerColor(C));
            }
        }

        private ArtColor CheckerColor(Coord loc)
        {
            int red, green, blue;
            if ((loc.row.FloorDivide(32) % 2) != (loc.col.FloorDivide(32) % 2))
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
            var buffer = new Grid<ArtColor>(size);
            foreach (var C in size.EachPoint())
            {
                var neighbors = new List<Coord>();
                if (C.row > 0)
                {
                    neighbors.Add(C.Plus(new Coord(-1, 0)));
                }
                if (C.row < size.row - 1)
                {
                    neighbors.Add(C.Plus(new Coord(1, 0)));
                }
                if (C.col > 0)
                {
                    neighbors.Add(C.Plus(new Coord(0, -1)));
                }
                if (C.col < size.col - 1)
                {
                    neighbors.Add(C.Plus(new Coord(0, 1)));
                }

                List<ArtColor> neighborColors = new List<ArtColor>();
                foreach (var n in neighbors)
                {
                    neighborColors.Add(canvas.GetCell(n));
                }
                int red = neighborColors.Select(z => z.red).Cast<int>().Sum() / neighborColors.Count();
                int green = neighborColors.Select(z => z.green).Cast<int>().Sum() / neighborColors.Count();
                int blue = neighborColors.Select(z => z.blue).Sum() / neighborColors.Count();
                buffer.SetCell(C, new ArtColor(red, green, blue));
            }
            canvas = buffer;
        }

        private void fishEye()
        {
            var buffer = new Grid<ArtColor>(size);
            foreach (var C in size.EachPoint())
            {
                var fishbowl = C.FloorDivide(20).Plus(C);
                buffer.SetCell(C, canvas.GetCell(fishbowl));
            }
            canvas = buffer;
        }

        private Tuple<int, int, int> spinColor(int red, int green, int blue)
        {
            int temp = red;
            red = green;
            green = blue;
            blue = temp;

            return new Tuple<int, int, int>(red, green, blue);
        }

        private ArtColor funColor2(Coord loc, bool isHilbert)
        {
            int red = colorDepth.FloorDivide(size.row) * loc.row;
            int green = (loc.row % gridSize * 8);
            int blue = (loc.col % gridSize * 8);



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
            int red = colorDepth.FloorDivide(size.col) * row;
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
            int hilbertsize = 1;

            int maxDimension = (int)Math.Floor(Math.Max(size.row, size.col) / (double)gridSize);
            while (hilbertsize < maxDimension)
            {
                hilbertsize *= 4;
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

        public Coord(int _square)
        {
            row = _square;
            col = _square;
        }

        public bool InRange(Coord topLeft, Coord bottomRight)
        {
            if (topLeft.row <= row && bottomRight.row >= row
                && topLeft.col <= col && bottomRight.col >= col)
            {
                return true;
            }
            return false;
        }
        public bool Clamp(Coord lowerBound, Coord upperBound)
        {
            var result = false;
            if (row < lowerBound.row)
            {
                result = true;
                row = lowerBound.row;
            }
            if (col < lowerBound.col)
            {
                result = true;
                col = lowerBound.col;
            }
            if (row > upperBound.row)
            {
                result = true;
                row = upperBound.row;
            }
            if (col > upperBound.col)
            {
                result = true;
                col = upperBound.col;
            }
            return result;
        }
        public Coord Times(int factor)
        {
            return new Coord(row * factor, col * factor);
        }
        public Coord Plus(Coord offset)
        {
            return new Coord(row + offset.row, col + offset.col);
        }
        public Coord FloorDivide(int factor)
        {
            return new Coord(row.FloorDivide(factor), col.FloorDivide(factor));
        }
    }

    public class ArtColor
    {
        private int _red, _green, _blue, colorRange;
        public int red { get => _red; 
            set { _red = value.Clamp(0, colorRange); } }
        public int green { get => _green; set { _green = value.Clamp(0, colorRange); } }
        public int blue { get => _blue; set { _blue = value.Clamp(0, colorRange); } }

        public ArtColor(int r, int g, int b, int _colorRange = 256)
        {
            colorRange = _colorRange - 1;
            red = r;
            green = g;
            blue = b;
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
        private Coord _loc;
        public Coord loc { get { return _loc; } set { } }
        public T value;
        public Cell(Coord c)
        {
            _loc = c;
            value = new T();
        }
    }

    public class Grid<T> where T : new()
    {
        private Coord _gridSize;
        public Coord gridSize { get { return _gridSize; } set { } }
        List<List<Cell<T>>> grid;
        public Grid(Coord param_gridSize)
        {
            _gridSize = param_gridSize;
            grid = new List<List<Cell<T>>>();
            for (int row = 0; row < gridSize.row; row++)
            {
                var gridRow = new List<Cell<T>>();
                for (int col = 0; col < gridSize.col; col++)
                {
                    gridRow.Add(new Cell<T>(new Coord(row, col)));
                }
                grid.Add(gridRow);
            }
        }

        public bool SetCell(Coord loc, T value)
        {
            if (InFrame(loc))
            {
                grid[loc.row][loc.col].value = value;
                return true;
            }
            return false;
        }

        public T GetCell(Coord loc)
        {
            if (InFrame(loc))
            {
                return grid[loc.row][loc.col].value;
            }
            throw new Exception("Coordinates out of range");
        }

        public bool InFrame(Coord loc)
        {
            return loc.InRange(new Coord(0,0), gridSize.Plus(new Coord(-1,-1)));
        }

        public Grid<T> Crop(Coord topLeft, Coord size)
        {
            var result = new Grid<T>(size);
            foreach (var C in Methods.EachPoint(size))
            {
                var offset = new Coord(topLeft.row + C.row, topLeft.col + C.col);
                if (!InFrame(offset))
                {
                    result.SetCell(C, new T());
                }
                else
                {
                    result.SetCell(C, GetCell(offset));
                }
            }     
            return result;
        }

        public List<Cell<T>> EachCell()
        {
            return grid.SelectMany(x => x).ToList();
        }

        public List<Coord> EachPoint()
        {
            return EachCell().Select(x => x.loc).ToList();
        }

        // to do: "blend" action that's like stamp, but calls a method on T for resolving the interaction
        //      also: revisit this method when adding opacity
        public bool Stamp(Coord topLeft, Grid<T> grid)
        {
            foreach (var C in grid.EachPoint())
            {
                Coord offset = topLeft.Plus(C);
                if (InFrame(offset))
                {
                    SetCell(offset, grid.GetCell(C));
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

            var result = new Grid<bool>(new Coord(3, 3));
            // corners are always true
            result.SetCell(new Coord(0, 0), true); // top left
            result.SetCell(new Coord(0, 2), true); // top right
            result.SetCell(new Coord(2, 0), true); // bottom left
            result.SetCell(new Coord(2, 2), true); // bottom right
            if (val == HilbertCurveComponent.A) // bottom missing
            {
                result.SetCell(new Coord(0, 1), true); // top middle
                result.SetCell(new Coord(1, 0), true); // left middle
                result.SetCell(new Coord(1, 2), true); // right middle
            } else if (val == HilbertCurveComponent.B) // right missing
            {
                result.SetCell(new Coord(0, 1), true); // top middle
                result.SetCell(new Coord(1, 0), true); // left middle
                result.SetCell(new Coord(2, 1), true); // bottom middle
            } else if (val == HilbertCurveComponent.C) // top missing
            {
                result.SetCell(new Coord(1, 0), true); // left middle
                result.SetCell(new Coord(1, 2), true); // right middle
                result.SetCell(new Coord(2, 1), true); // bottom middle
            } else if (val == HilbertCurveComponent.D) // left missing
            {
                result.SetCell(new Coord(0, 1), true); // top middle
                result.SetCell(new Coord(1, 2), true); // right middle
                result.SetCell(new Coord(2, 1), true); // bottom middle
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
            return list.ToGrid(new Coord(2));
        }

        private static Grid<HilbertCurveComponent> IterateHilbertCurve(Grid<HilbertCurveComponent> start)
        {
            var finish = new Grid<HilbertCurveComponent>(start.gridSize.Times(2));
            foreach (var C in start.EachCell())
            {
                finish.Stamp(C.loc.Times(2), IterateCurveComponent(C.value));
            }
            return finish;
        }

        private static Grid<HilbertCurveComponent> initialState(HilbertCurveComponent orientation)
        {
            var result = new Grid<HilbertCurveComponent>(new Coord(1,1));
            result.SetCell(new Coord(0,0), orientation);
            return result;
        }

        private static Grid<bool> ResolveCurve(Grid<HilbertCurveComponent> curve)
        {
            // 1. render components to bools
            var result = new Grid<bool>(curve.gridSize.Times(4));
            foreach (var C in curve.EachCell())
            {
                result.Stamp(C.loc.Times(4), ResolveComponent(C.value));
            }

            // 2. connect the blocks
            foreach (var C in result.EachCell())
            {
                if (C.value == true && CountNeighbors(result, C.loc) == 1)
                {
                    if (C.loc.row > 1) // check left connection
                    {
                        if (CountNeighbors(result, C.loc.Plus(new Coord(-2, 0))) == 1 && curve.GetCell(C.loc.FloorDivide(4)) != HilbertCurveComponent.D)
                        {
                            result.SetCell(C.loc.Plus(new Coord(-1, 0)), true);
                        }
                    }
                    if (C.loc.row < result.gridSize.row - 2) // check right connection
                    {
                        if (CountNeighbors(result, C.loc.Plus(new Coord(2, 0))) == 1 && curve.GetCell(C.loc.FloorDivide(4)) != HilbertCurveComponent.B)
                        {
                            result.SetCell(C.loc.Plus(new Coord(1, 0)), true);
                        }
                    }
                    if (C.loc.col > 1) // check up connection
                    {
                        if (CountNeighbors(result, C.loc.Plus(new Coord(0, -2))) == 1 && curve.GetCell(C.loc.FloorDivide(4)) != HilbertCurveComponent.C)
                        {
                            result.SetCell(C.loc.Plus(new Coord(0, -1)), true);
                        }
                    }
                    if (C.loc.col < result.gridSize.col - 2) // check down connection
                    {
                        if (CountNeighbors(result, C.loc.Plus(new Coord(0, 2))) == 1 && curve.GetCell(C.loc.FloorDivide(4)) != HilbertCurveComponent.A)
                        {
                            result.SetCell(C.loc.Plus(new Coord(0, 1)), true);
                        }
                    }
                }
            }
            return result;
        }

        private static int CountNeighbors(Grid<bool> grid, Coord loc)
        {
            int count = 0;
            if (loc.row > 0)
            {
                count += grid.GetCell(loc.Plus(new Coord(-1, 0))) == true ? 1 : 0;
            }
            if (loc.row < grid.gridSize.row - 1)
            {
                count += grid.GetCell(loc.Plus(new Coord(1, 0))) == true ? 1 : 0;
            }
            if (loc.col > 0)
            {
                count += grid.GetCell(loc.Plus(new Coord(0, -1))) == true ? 1 : 0;
            }
            if (loc.col < grid.gridSize.col - 1)
            {
                count += grid.GetCell(loc.Plus(new Coord(0, 1))) == true ? 1 : 0;
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

        public static List<Coord> EachPoint(this Coord range)
        {
            var result = new List<Coord>();
            for (int row = 0; row < range.row; row++)
            {
                for (int col = 0; col < range.col; col++)
                {
                    result.Add(new Coord(row, col));
                }
            }
            return result;
        }

        public static List<List<T>> InitializeRect<T>(this List<List<T>> rect, Coord size) where T : new()
        {
            var result = new List<List<T>>();
            for (int row = 0; row < size.row; row++)
            {
                var r = new List<T>();
                for (int col = 0; col < size.col; col++)
                {
                    r.Add(new T());
                }
                result.Add(r);
            }
            return result;
        }

        public static Grid<T> ToGrid<T>(this List<T> list, Coord size) where T : new()
        {
            var grid = new Grid<T>(size);
            foreach(var C in grid.EachCell())
            {
                C.value = list[(C.loc.row * size.col) + C.loc.col];
            }
            return grid;
        }
    }
}
