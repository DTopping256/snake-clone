using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Flat.Graphics;
using Flat.Input;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Diagnostics;

namespace SnakeClone;

enum Direction {
    Up,
    Down,
    Left,
    Right,
}

class SnakeEntity {
    // Snake update state
    private readonly Dictionary<Direction, (int, int)> snakeMovementMap = new()
    {
        { Direction.Up, (0, 1) },
        { Direction.Right, (1, 0) },
        { Direction.Down, (0, -1) },
        { Direction.Left, (-1, 0) }
    };
    private const int START_SIZE = 8; 
    
    private readonly FlatKeyboard kb = FlatKeyboard.Instance;

    // Snake state
    private Direction direction;
    private int size = START_SIZE;
    private List<(int, int)> segments;

    // Extras for drawing
    private int maxCellsX;
    private int maxCellsY;
    private int gridOriginX;
    private int gridOriginY;
    private int cellSize;
    // For creating a stepped movement
    private float cooldown;
    private float speedMultiplier = 4;
    private float maxSpeedMultiplier = 17.5f;

    public List<(int, int)> GetSegments() {
        return [.. segments];
    }

    public SnakeEntity(int startX, int startY, Direction direction, int maxCellsX, int maxCellsY, int gridOriginX, int gridOriginY, int cellSize) {
        this.direction = direction;
        segments = new List<(int, int)>([(startX, startY)]);
        this.maxCellsX = maxCellsX;
        this.maxCellsY = maxCellsY;
        this.gridOriginX = gridOriginX;
        this.gridOriginY = gridOriginY;
        this.cellSize = cellSize;
        cooldown = cellSize;
    }

    public string DebugInfo() {
        string debugMessage = "";
        string[] vectorStrings = new string[segments.Count];
        for (int i = 0; i < segments.Count; i++) {
            var (x, y) = segments[i];
            vectorStrings[i] = "(" + x.ToString() + ", " + y.ToString() + ")";
        }
        debugMessage += "\n\tSize: " + size.ToString();
        debugMessage += "\n\tSpeed multiplier: " + speedMultiplier.ToString();
        debugMessage += "\n\tSegments: " + string.Join(", ", vectorStrings);
        return debugMessage;
    }

    private bool IsFacing(Direction direction) {
        if (direction == Direction.Up) {
            var (x1, y1) = segments[0];
            var (x2, y2) = segments[1];
            return x1 == x2 && y1 > y2;
        }
        if (direction == Direction.Right) {
            var (x1, y1) = segments[0];
            var (x2, y2) = segments[1];
            return x1 > x2 && y1 == y2;
        }
        if (direction == Direction.Down) {
            var (x1, y1) = segments[0];
            var (x2, y2) = segments[1];
            return x1 == x2 && y1 < y2;
        }
        if (direction == Direction.Left) {
            var (x1, y1) = segments[0];
            var (x2, y2) = segments[1];
            return x1 < x2 && y1 == y2;
        }
        return false;
    }

    private float GetSpeedIncrease(int level) {
        return 2f / level + 0.2f;
    }

    public void IncreaseDifficulty(int level) {
        size += 3;
        speedMultiplier += GetSpeedIncrease(level);
        if (speedMultiplier > maxSpeedMultiplier) {
            speedMultiplier = maxSpeedMultiplier;
        }
    }

    public void Update(float baseSpeed) {
        if (kb.IsKeyDown(Keys.Up) && (IsFacing(Direction.Left) || IsFacing(Direction.Right))) {
            direction = Direction.Up;
        }
        if (kb.IsKeyDown(Keys.Right) && (IsFacing(Direction.Up) || IsFacing(Direction.Down))) {
            direction = Direction.Right;
        }
        if (kb.IsKeyDown(Keys.Down) && (IsFacing(Direction.Left) || IsFacing(Direction.Right))) {
            direction = Direction.Down;
        }
        if (kb.IsKeyDown(Keys.Left) && (IsFacing(Direction.Up) || IsFacing(Direction.Down))) {
            direction = Direction.Left;
        }
        
        // Snake should move in steps, a cooldown stops the snake from gliding.
        cooldown -= baseSpeed * speedMultiplier;
        if (cooldown < 0) {
            cooldown = cellSize;

            // Adds a new head segment in the current direction.
            var (x, y) = segments[0]; 
            (int, int) diff;
            if (!snakeMovementMap.TryGetValue(direction, out diff)) {
                throw new Exception("Invalid direction");
            }
            var (diffX, diffY) = diff;
            int maxX = maxCellsX / 2;
            int minX = -1 * maxCellsX / 2;
            int maxY = maxCellsY / 2;
            int minY = -1 * maxCellsY / 2;
            int newX = x + diffX;
            if (newX > maxX) {
                newX = minX;
            } else if (newX < minX) {
                newX = maxX;
            }
            int newY = y + diffY;
            if (newY > maxY) {
                newY = minY;
            } else if (newY < minY) {
                newY = maxY;
            }
            segments = [.. segments.Prepend((newX, newY))];

            // Removes old tail segment 
            if (segments.Count > size) {
                segments.RemoveAt(segments.Count - 1);
            }
        }
    }

    public void Draw(Shapes shapes) {
        for (int i = 0; i < segments.Count; i++)
        {
            var (x, y) = segments[i];
            x += gridOriginX;
            y += gridOriginY;

            // Create points of a square the size of a cell.
            int x1 = x * cellSize;
            int x2 = (x + 1) * cellSize;
            int y1 = y * cellSize;
            int y2 = (y + 1) * cellSize;
            
            // Draw a quad with points given in clockwise order.
            shapes.DrawQuadFill(x1, y1, x1, y2, x2, y2, x2, y1, Color.Green);
        }
    }
}

class AppleEntity {
    private readonly Random rnd;
    private SnakeEntity snake;

    // State
    private (int, int) gridPosition;

    // Extras for drawing
    private int maxCellsX;
    private int maxCellsY;
    private int gridOriginX;
    private int gridOriginY;
    private int cellSize;

    public AppleEntity(SnakeEntity snake, int maxCellsX, int maxCellsY, int gridOriginX, int gridOriginY, int cellSize) {
        rnd = new Random();
        this.snake = snake;
        this.maxCellsX = maxCellsX;
        this.maxCellsY = maxCellsY;
        this.gridOriginX = gridOriginX;
        this.gridOriginY = gridOriginY;
        this.cellSize = cellSize;
        Move();
    }

    public (int, int) Position() {
        return (gridPosition.Item1, gridPosition.Item2);
    }

    public string DebugInfo() {
        var (x, y) = gridPosition;
        return "\n\tPosition: (" + x.ToString() + ", " + y.ToString() + ")";
    }

    private bool isValidPlacement(int ax, int ay) {
        foreach (var (sx, sy) in snake.GetSegments()) {
            if (ax == sx && ay == sy) {
                return false;
            }
        }
        return true;
    }

    public void Move() {
        int x, y;
        do {
            x = rnd.Next(maxCellsX) - gridOriginX;
            y = rnd.Next(maxCellsY) - gridOriginY;
        }
        while (!isValidPlacement(x, y));
        gridPosition = (x, y);
    }

    public void Draw(Shapes shapes) {
        var (x, y) = gridPosition;
        shapes.DrawCircleFill((x + gridOriginX) * cellSize + (cellSize / 2), (y + gridOriginY) * cellSize + (cellSize / 2), cellSize / 2 + 2, 12, Color.BlueViolet);
    }
}

public class Game1 : Game
{

    private GraphicsDeviceManager graphics;
    private Screen screen;
    private Shapes shapes;
    

    private Sprites sprites;

    private SnakeEntity snake;
    private AppleEntity apple;
    
    private int cellSize;
    private int maxCellsX;
    private int maxCellsY;
    private int level;

    public Game1()
    {
        graphics = new GraphicsDeviceManager(this);
        graphics.SynchronizeWithVerticalRetrace = true;

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        IsFixedTimeStep = true;
    }

    private bool hasSnakeHeadCollidedWithApple() {
        var (sx, sy) = snake.GetSegments()[0];
        var (ax, ay) = apple.Position();
        return sx == ax && sy == ay;
    }

    protected override void Initialize()
    {
        screen = new Screen(this, 1920, 1080);
        shapes = new Shapes(this);
        sprites = new Sprites(this);

        cellSize = 30;
        maxCellsX = screen.Width / cellSize;
        maxCellsY = (screen.Height - 200) / cellSize;
        int gridOriginX = maxCellsX / 2;
        int gridOriginY = maxCellsY / 2;

        level = 1;
        snake = new SnakeEntity(0, 0, Direction.Right, maxCellsX, maxCellsY, gridOriginX, gridOriginY, cellSize);
        apple = new AppleEntity(snake, maxCellsX, maxCellsY, gridOriginX, gridOriginY, cellSize);
        base.Initialize();
    }

    protected override void LoadContent()
    {
    }

    protected override void Update(GameTime gameTime)
    {
        int timepassed = gameTime.ElapsedGameTime.Milliseconds;
        float snakeSpeed = timepassed / 20f;

        FlatKeyboard kb = FlatKeyboard.Instance;
        kb.Update();

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || kb.IsKeyDown(Keys.Escape))
        {
            Exit();
        } 

        if (kb.IsKeyClicked(Keys.D))
        {
            Debug.WriteLine("Level: " + level.ToString());
            Debug.WriteLine("Snake state: " + snake.DebugInfo());
            Debug.WriteLine("Apple state: " + apple.DebugInfo());
        }

        snake.Update(snakeSpeed);

        if (hasSnakeHeadCollidedWithApple()) {
            apple.Move();
            snake.IncreaseDifficulty(level);
            level++;
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        screen.Set();
        GraphicsDevice.Clear(Color.Black);

        shapes.Begin();
        snake.Draw(shapes);
        apple.Draw(shapes);
        shapes.End();

        screen.Unset();
        screen.Present(sprites);

        base.Draw(gameTime);
    }
}
