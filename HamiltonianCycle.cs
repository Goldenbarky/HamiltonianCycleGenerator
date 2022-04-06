
class HamiltonianCycle {

    public enum Direction { Left, Down, Right, Up }
    public static void Main(string []args) {
        
        int width;
        if (args.Length > 0) {
            if (!int.TryParse( args[ 0 ], out width)) {
                width = GetUserInput("Enter width: ");
            }
        }
        else {
            width = GetUserInput("Enter width: ");
        }
        
        int height;
        if (args.Length > 1) {
            if (!int.TryParse(args[ 1 ], out height)) {
                height = GetUserInput("Enter height: ");
            }
        } 
        else {
            height = GetUserInput("Enter height: ");
        }
        
        int[,] grid = CreateHamiltonianGuide(height, width);

        Console.WriteLine("Drawing pathway...");
        if (FollowGuide(0, 0, 1, Direction.Right, grid) is var values) {
            if (!values.Item1) {
                Console.WriteLine("Failiure >:(");
            }
        }

        if ((args.Any(s=>s.Equals("--verbose") || s.Equals("-v")))) {
            Console.WriteLine("Hamiltonian Guide:");
            PrintGrid(grid, false);
        }

        grid = CleanUpGrid(grid);
        
        string filePath = $"{width}x{height}.txt";
        using (StreamWriter fs = new StreamWriter(filePath)) {
            if ( args.Any( s => s.Equals( "--order" ) || s.Equals( "-O" ) ) ) {
                // Print the cell order in which to go
                for ( int i = 0; i < width * height; ++i ) {
                    // + 1 is redundant atm
                    fs.Write($"{(values.Item2[i].Item2 + 1) / 2},{(values.Item2[i].Item1 + 1) / 2} ");
                }
            }
            else {
                for ( int i = 0; i < height; i++ ) {
                    for ( int j = 0; j < width; j++ ) {
                        fs.Write( $"{grid[ i, j ],8}" );
                    }

                    fs.Write( "\n" );
                }
            }
        }
        
        Console.WriteLine($"Hamiltonian Cycle found! Created {filePath}");
    }

    static int GetUserInput(string message) {
        int userInput;
        bool done = false;
        do {
            Console.Write(message);
            if (int.TryParse(Console.ReadLine(), out userInput)) {
                done = true;
            }
        } while (!done);

        return userInput;
    }

    public static (int, int) ConvertDirection(Direction dir) {
        int xDir = 0, yDir = 0;
        
        switch(dir) {
            case Direction.Up:
                xDir = -1;
                yDir = 0;
                break;
            case Direction.Down:
                xDir = 1;
                yDir = 0;
                break;
            case Direction.Left:
                xDir = 0;
                yDir = -1;
                break;
            case Direction.Right:
                xDir = 0;
                yDir = 1;
                break;
        }

        return (xDir, yDir);
    }

    public static void PrintGrid(int[,] grid, bool unformatted) {
        int length = grid.GetLength(0);
        int width = grid.GetLength(1);

        for(int i = 0; i < length; i++) {
            for(int j = 0; j < width; j++) {
                if(unformatted || grid[i, j] >= 0 || grid[i, j] != -2 && (i == 0 || i == length - 1 || j == 0 || j == width - 1 || grid[i, j - 1] != -2 || grid[i, j + 1] != -2 || grid[i + 1, j] != -2 || grid[i - 1, j] != -2))
                    Console.Write($"[{grid[i,j]}]\t");
                else 
                    Console.Write("\t");
            }
            Console.Write("\n\n");
        }
        Console.Write("\n\n");
    }

    //Create the pseudo maze for the pathfinding algorithm to solve
    public static int[,] CreateHamiltonianGuide(int baseX, int baseY) {
        int[,] grid = new int[baseX * 2 - 1, baseY * 2 - 1];
        int[,] walls = new int[baseX - 1, baseY - 1];

        Console.WriteLine("Populating edges...");
        PopulateEdges(walls);
        Console.WriteLine("Generating spanning tree...");
        MinimallySpanningTree(walls);

        //Console.WriteLine("Spanning tree:");
        //PrintGrid(walls, false);

        Console.WriteLine("Finalizing Guide...");
        TranslateWallsToGrid(grid, walls);

        return grid;
    }

    //Generates randomly weighted edges between each node in the grid
    public static void PopulateEdges(int[,] grid) {
        int length = grid.GetLength(0);
        int width = grid.GetLength(1);

        Random random = new Random();

        for(int x = 0; x < length; x++) {
            for(int y = 0; y < width; y++) {
                if(x % 2 == 1 && y % 2 == 1) grid[x, y] = -1;
                else if(x % 2 == 1 ^ y % 2 == 1) grid[x, y] = random.Next(1, 500);
            }
        }
    } 

    //Remove edges to create a minimally spanning tree
    public static void MinimallySpanningTree(int[,] grid) {
        int length = grid.GetLength(0);
        int width = grid.GetLength(1);

        for(int x = 0; x < length; x += 2) {
            for(int y = 0; y < width; y += 2) {
                if(x == length - 1 && y == width - 1) break;

                int i = 0;
                int j = 0;

                if(x + 1 < length && (y + 1 >= width || grid[x + 1, y] > grid[x, y + 1]))
                    i = 1;
                else 
                    j = 1;

                int prevValue = grid[x + i, y + j];
                grid[x + i, y + j] = -1;

                bool successful = false;

                if(y + j % 2 == 0) {
                    PriorityQueue<(int, int), int> nodes = new PriorityQueue<(int, int), int>();

                    if(x + i + 1 < length) {
                        nodes.Enqueue((x + i + 1, y + j), 0);
                        successful = CheckCanReachHome(grid, nodes);
                    }

                    nodes.Clear();

                    if(successful && x + i - 1 >= 0) {
                        nodes.Enqueue((x + i - 1, y + j), 0);
                        successful = CheckCanReachHome(grid, nodes);
                    }
                } else {
                    PriorityQueue<(int, int), int> nodes = new PriorityQueue<(int, int), int>();

                    
                    if(y + j + 1 < width) {
                        nodes.Enqueue((x + i, y + j + 1), 0);
                        successful = CheckCanReachHome(grid, nodes);
                    }

                    nodes.Clear();

                    if(successful && y + j - 1 >= 0) {
                        nodes.Enqueue((x + i, y + j - 1), 0);
                        successful = CheckCanReachHome(grid, nodes);
                    }
                }

                if(!successful) {
                    grid[x + i, y + j] = prevValue;
                }
            }
        }
    }

    //Check if still connected to 0,0 with dijkstra's algorithm
    public static bool CheckCanReachHome(int[,] grid, PriorityQueue<(int, int), int> nodes) {
        int length = grid.GetLength(0);
        int width = grid.GetLength(1);
        HashSet<(int, int)> visited = new HashSet<(int, int)>();

        while(nodes.Count > 0) {
            var (x, y) = nodes.Dequeue();

            //Return true if home
            if(x == 0 && y == 0) return true;

            //Iterate over the 4 cardinal directions
            for(int i = -1; i < 2; i++) {
                for(int j = -1; j < 2; j += 2) {
                    if(i != 0) j = 0;

                    //If in bounds and not -1 enqeue the node with the priority of its distance from 0,0
                    if(x + i >= 0 && x + i < length && y + j >= 0 && y + j < width && grid[x + i, y + j] != -1 && !visited.Contains((x + i, y + j))) {
                        nodes.Enqueue((x + i, y + j), (x + i) + (y + j));
                        visited.Add((x + i, y + j));
                    }
                }
            }
        }
        
        //If no remaining nodes to check node is severed
        return false;
    }

    //Move the minmally spanning tree edges to walls in the grid
    public static void TranslateWallsToGrid(int[,] grid, int[,] walls) {
        int lenWalls = walls.GetLength(0);
        int widthWalls = walls.GetLength(1);

        int lenGrid = grid.GetLength(0);
        int widthGrid = grid.GetLength(1);

        //Populate grid with 0 for final nodes, -1 for walls between nodes and -2 for useless spaces
        for(int x = 0; x < lenGrid; x++) {
            for(int y = 0; y < widthGrid; y++)
            {
                grid[x, y] = ( x % 2 , y % 2 ) switch {
                    (0,0) => 0,
                    (1,1) => -1,
                    _ => -2,
                };
            }
        }

        //Turn each wall in the tree into it's 2 grid counterparts
        for(int x = 0; x < lenWalls; x++) {
            for(int y = 0; y < widthWalls; y++) {
                switch ( (x % 2, y % 2) )
                {
                    case (1, 1):
                        continue;
                    case (0, 0):
                        grid[x * 2 + 1, y * 2 + 1] = -1;
                        continue;
                }

                if(walls[x, y] == -1) continue;

                if(x % 2 == 0) {
                    grid[x * 2 + 1, y * 2] = -1;
                    grid[x * 2 + 1, y * 2 + 2] = -1;
                } else {
                    grid[x * 2, y * 2 + 1] = -1;
                    grid[x * 2 + 2, y * 2 + 1] = -1;
                }
            }
        }


        //Changed useless -1 walls to -2 to represent useless spaces
        for(int x = 0; x < lenGrid; x++) {
            for(int y = 0; y < widthGrid; y++) {
                if(x != 0 && x != lenGrid - 1 && y != 0 && y != widthGrid - 1 && grid[x, y] != 0 && grid[x, y - 1] == -2 && grid[x, y + 1] == -2 && grid[x + 1, y] == -2 && grid[x - 1, y] == -2) 
                    grid[x, y] = -2;
            }
        }

        
    }

    public static (bool, (int,int)[]) FollowGuide(int x, int y, int currNum, Direction dir, int[,] grid) {
        int length = grid.GetLength(0);
        int width = grid.GetLength(1);

        (int, int)[] orderVals = new (int, int)[length * width];

        while(currNum <= ((length + 1) / 2) * ((width + 1) / 2)) {
            //Locate direction the wall should be in
            var (i, j) = ConvertDirection(ComplementDirection(dir));

            //Base case
            //If wall is not to our right, we're lost
            bool lost;
            if(x + i >= length || x + i < 0 || y + j >= width || y + j < 0 || grid[x, y] == -1) {
                (i, j) = ConvertDirection(dir);
                x -= i;
                y -= j;
                lost = true;
            }
            //If out of bounds or hit wall, we're lost
            else if(!((x == 0 || x == length - 1) && (y == 0 || y == width - 1)) && grid[x + i, y + j] != -1)
                lost = true;
            else 
                lost = false;
            
            //If current tile is a node number it
            if(grid[x, y] == 0)
            {
                orderVals[currNum - 1] = (x, y);
                grid[x, y] = currNum++;
                //PrintGrid(grid, false);
            }

            //If lost choose new direction
            if(lost) {
                //If hit a wall, invert wall direction for new dir
                //Otherwise move in the direction the wall previously was
                dir = (x + i < length && x + i >= 0 && y + j < width && y + j >= 0 && grid[x + i, y + j] == -1) ? InvertDirection(ComplementDirection(dir)) : ComplementDirection(dir);
            }

            (i, j) = ConvertDirection(dir);
            x += i;
            y += j;
        }

        return (true, orderVals);
    }

    public static bool FollowGuide2(int x, int y, int currNum, Direction dir, bool lost, int[,] grid) {
        int length = grid.GetLength(0);
        int width = grid.GetLength(1);

        //Check every direction starting with current direction
        Direction nextDir = dir;
        do {
            if((currNum > ((length + 1) / 2) * ((width + 1) / 2))) return true;
          
            var (i, j) = ConvertDirection(ComplementDirection(nextDir));
            if(!(x >= length || x < 0 || y >= width || y < 0 || (grid[x, y] != 0 && grid[x, y] != -2))) {

                if(!(x + i >= length || x + i < 0 || y + j >= width || y + j < 0)) {
                    if(!((x == 0 || x == length - 1) && (y == 0 || y == width - 1)) && grid[x + i, y + j] != -1) {
                        if(!lost) lost = true;
                        else return false;
                    } else 
                        lost = false;

                    if(grid[x, y] == 0) {
                        grid[x, y] = currNum++;
                    }
                }
            }
            dir = nextDir;
            nextDir = ComplementDirection(nextDir);
            x += i;
            y += j;
        } while(nextDir != dir); 

        return false;
    }

    public static Direction ComplementDirection(Direction dir) => dir switch {
            Direction.Right => Direction.Down,
            Direction.Down => Direction.Left,
            Direction.Left => Direction.Up,
            Direction.Up => Direction.Right,
            _ => throw new ArgumentOutOfRangeException(nameof(dir), dir, null),
        };

    public static Direction InvertDirection(Direction dir) => dir switch {
            Direction.Right => Direction.Left,
            Direction.Down => Direction.Up,
            Direction.Left => Direction.Right,
            Direction.Up => Direction.Down,
            _ => throw new ArgumentOutOfRangeException(nameof(dir), dir, null),
        };


    public static int[,] CleanUpGrid(int[,] grid) {
        int length = grid.GetLength(0);
        int width = grid.GetLength(1);

        int newLength = (length + 1) / 2;
        int newWidth = (width + 1) / 2;

        int[,] newGrid = new int[newLength, newWidth];

        for(int x = 0; x < length; x++) {
            for(int  y = 0; y < width; y++) {
                if(x % 2 == 0 && y % 2 == 0) {
                    newGrid[x / 2, y / 2] = grid[x, y];
                }
            }
        }

        return newGrid;
    }
}
