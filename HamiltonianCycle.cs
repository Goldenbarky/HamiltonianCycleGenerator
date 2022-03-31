class HamiltonianCycle {

    public enum Direction { Left, Down, Right, Up };
    public static void Main(string []args) {
        if(args.Length < 2) return;

        int len = int.Parse(args[0]);
        int width = int.Parse(args[1]);
        
        int[,] grid = CreateHamiltonianGuide(len, width);
        
        //Console.WriteLine("Hamiltonian Guide:");
        //PrintGrid(grid, false);

        Console.WriteLine("Drawing pathway...");
        if(!FollowGuide(0, 0, 1, Direction.Right, grid))
            Console.WriteLine("Failiure >:(");
        
        //PrintGrid(grid, false);

        grid = CleanUpGrid(grid);
        
        string filePath = $"{len}x{width}.txt";
        using (StreamWriter fs = new StreamWriter(filePath)) {
            for(int i = 0; i < len; i++) {
                for(int j = 0; j < width; j++) {
                    fs.Write($"{grid[i, j], 8}");
                }
                fs.Write("\n");
            }
        }
        
        Console.WriteLine($"Hamiltonian Cycle found! Created {filePath}");
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

        for(int x = 0; x < lenGrid; x++) {
            for(int y = 0; y < widthGrid; y++) {
                if(x % 2 == 0 && y % 2 == 0) {
                    grid[x,y] = 0;
                } else if (x % 2 == 1 && y % 2 == 1) {
                    grid[x, y] = -1;
                } else {
                    grid[x, y] = -2;
                }
            }
        }

        for(int x = 0; x < lenWalls; x++) {
            for(int y = 0; y < widthWalls; y++) {
                if(x % 2 == 1 && y % 2 == 1) continue;
                else if(x % 2 == 0 && y % 2 == 0) {
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

        for(int x = 0; x < lenGrid; x++) {
            for(int y = 0; y < widthGrid; y++) {
                if(x != 0 && x != lenGrid - 1 && y != 0 && y != widthGrid - 1 && grid[x, y] != 0 && grid[x, y - 1] == -2 && grid[x, y + 1] == -2 && grid[x + 1, y] == -2 && grid[x - 1, y] == -2) 
                    grid[x, y] = -2;
            }
        }

        
    }

    public static bool FollowGuide(int x, int y, int currNum, Direction dir, int[,] grid) {
        int length = grid.GetLength(0);
        int width = grid.GetLength(1);

        bool lost = false;

        while(currNum <= ((length + 1) / 2) * ((width + 1) / 2)) {
            //Locate direction the wall should be in
            var (i, j) = ConvertDirection(ComplementDirection(dir));

            //Base case
            //If wall is not to our right, we're lost
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
            if(grid[x, y] == 0) {
                grid[x, y] = currNum++;
                //PrintGrid(grid, false);
            }

            //If lost choose new direction
            if(lost) {
                //If hit a wall, invert wall direction for new dir
                //Otherwise move in the direction the wall previously was
                dir = (x + i < length && x + i >= 0 && y + j < width && y + j >= 0 && grid[x + i, y + j] == -1) ? InvertDirection(ComplementDirection(dir)) : ComplementDirection(dir);
                lost = false;
            }

            (i, j) = ConvertDirection(dir);
            x += i;
            y += j;
        }

        return true;
    }

    public static Direction ComplementDirection(Direction dir) {
        Direction nextDir = dir switch {
            Direction.Right => Direction.Down,
            Direction.Down => Direction.Left,
            Direction.Left => Direction.Up,
            Direction.Up => Direction.Right,
        };

        return nextDir;
    }

    public static Direction InvertDirection(Direction dir) {
        Direction nextDir = dir switch {
            Direction.Right => Direction.Left,
            Direction.Down => Direction.Up,
            Direction.Left => Direction.Right,
            Direction.Up => Direction.Down,
        };

        return nextDir;
    }

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
