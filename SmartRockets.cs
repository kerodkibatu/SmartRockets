using Krane.Core;
using Krane.Extensions;
using Krane.Resources;
using SFML.Graphics;
using SFML.System;

class SmartRockets : Game
{
    int Gen = 0;
    int Counter = 0;
    List<Rocket> Rockets;
    List<CircleShape> Obstacles;
    int PopSize = 600;
    Vector2f SpawnPoint;
    Vector2f Goal;
    public SmartRockets() : base((500, 500), "Smart Rockets", 60)
    {
    }
    public override void Initialize()
    {
        SpawnPoint = new(WIDTH / 2, HEIGHT / 2);
        Goal = new Vector2f(WIDTH / 5, HEIGHT / 20);
        Rockets = new();
        Obstacles = new();

        for (int i = 0; i < PopSize; i++)
        {
            Rockets.Add(new Rocket().Initialize(SpawnPoint, Goal));
        }
    }
    float MaxFit = float.NegativeInfinity;
    public override void Update()
    {
        if (Input.IsKeyDown(SFML.Window.Keyboard.Key.G))
            Goal = Input.MousePos;
        if (Input.IsKeyDown(SFML.Window.Keyboard.Key.S))
            SpawnPoint = Input.MousePos;
        if (Counter % 30 == 0 && Input.IsKeyDown(SFML.Window.Keyboard.Key.O))
            Obstacles.Add(new CircleShape(20)
            {
                Position = Input.MousePos,
                FillColor = Color.Blue
            }.Center());
        for (int j = 0; j < (Input.IsKeyDown(SFML.Window.Keyboard.Key.F) ? Rocket.LifeTime : 1); j++)
        {
            foreach (var r in Rockets)
            {
                r.Update(Counter);
                foreach (var wall in Obstacles)
                    if (r.Body.GetGlobalBounds().Intersects(wall.GetGlobalBounds()))
                        r.Alive = false;
            }
            Counter++;
            if (Counter > Rocket.LifeTime)
            {
                // Check Fitness
                // Keep Top 10%
                // Reproduce
                MaxFit = 0f;
                foreach (var r in Rockets)
                {
                    var f = MeasureFitness(r, Goal);
                    if (f > MaxFit)
                        MaxFit = f;
                }
                SetTitle($"Max Fit: {MaxFit}");
                Rockets = Rockets.OrderBy(r => r.Fitness).ToList();
                var nextGen = new List<Rocket>();
                nextGen.AddRange(Rockets.Take((int)(PopSize * 0.1f)));

                var popPool = new List<Rocket>();
                foreach (var r in Rockets)
                {
                    r.Fitness /= MaxFit;
                    for (int i = 0; i < r.Fitness * PopSize; i++)
                    {
                        popPool.Add(r);
                    }
                }

                while (nextGen.Count < PopSize)
                {
                    var A = popPool[Random.Shared.Next(popPool.Count)];
                    var B = popPool[Random.Shared.Next(popPool.Count)];
                    if (A != B)
                    {
                        var child = A.Reproduce(B);
                        nextGen.Add(child);
                    }
                }
                Rockets = nextGen.Take(PopSize).ToList();
                foreach (var R in Rockets)
                {
                    R.Initialize(SpawnPoint, Goal, false);
                    R.Mutate(0.01f);
                }
                Counter = 0;
                Gen ++;
            }
        }
    }
    public override void Draw()
    {
        Render.Clear();
        foreach (var r in Rockets)
            r.Draw();
        foreach (var wall in Obstacles)
        {
            Render.Draw(wall);
        }
        Render.Draw(new CircleShape(10)
        {
            Position = Goal,
            FillColor = Color.Yellow,
        });
        Render.Draw(
            new Text($"Gen: {Gen}\nIter: {Counter}\nMaxFit: {MaxFit}",FontManager.Active)
        );
    }
    public float MeasureFitness(Rocket R, Vector2f Goal)
    {
        float Fitness = SpawnPoint.Distance(Goal) - R.Body.Position.Distance(Goal);
        if (!R.Alive)
            Fitness /= 10;
        if (R.Won)
            Fitness += 100 - R.TimeTaken;
        return R.Fitness = Fitness;
    }
}
class Rocket
{
    const float MaxForce = 1f;
    public const int LifeTime = 200;
    public RectangleShape Body;
    Vector2f[] Genes;
    Vector2f Vel;
    public float Fitness = 0;
    public bool Alive = true;
    public bool Won = false;
    public int TimeTaken = -1;
    public Rocket()
    {
        Body = new RectangleShape(size: new(25, 10));
        Body.Origin = Body.Size / 2;
        Genes = new Vector2f[LifeTime];
        Vel = new Vector2f();
    }
    public void RandomizeForces()
    {
        for (int i = 0; i < Genes.Length; i++)
        {
            Genes[i] = GetRandomForce();
            //Forces[i] = new Vector2f(0.1f,-0.1f);
        }
    }
    public Vector2f GetRandomForce()
    {
        var theta = Random.Shared.NextSingle() * MathF.Tau;
        return new Vector2f(MathF.Cos(theta), MathF.Sin(theta)) * MaxForce;
    }
    public void Place(Vector2f newPos) => Body.Position = newPos;
    public void Update(int counter)
    {
        if (counter >= LifeTime || !Alive || Won)
            return;
        Vel += Genes[counter];
        Body.Position += Vel;
        if (Body.Position.Distance(goal) < 10)
        {
            Won = true;
            TimeTaken = counter;
        }
        Body.Rotation = (Vel.Heading() * 90) + 180f;
        if (Body.Position.X < 0 || Body.Position.Y < 0 || Body.Position.X > Render.Target!.Size.X || Body.Position.Y > Render.Target!.Size.Y)
            Alive = false;
    }
    public void Draw()
    {
        if (!Alive)
            Body.FillColor = Color.Red;
        else
            Body.FillColor = Color.White;
        if (Won)
            Body.FillColor = Color.Green;
        Body.FillColor = new Color(Body.FillColor.R, Body.FillColor.G, Body.FillColor.B, 100);
        Render.Draw(Body);
    }

    public Rocket Reproduce(Rocket ParentB)
    {
        var ParentA = this;

        Rocket child = new();

        int mid = Random.Shared.Next(LifeTime);
        for (int i = 0; i < LifeTime; i++)
        {
            if (i < mid)
                child.Genes[i] = ParentA.Genes[i];
            else
                child.Genes[i] = ParentB.Genes[i];
        }
        return child;
    }
    public void Mutate(float Chance = 0.01f)
    {
        for (int i = 0; i < LifeTime; i++)
        {
            if (Random.Shared.NextSingle() < Chance)
                Genes[i] = GetRandomForce();
        }
    }
    Vector2f goal;
    internal Rocket Initialize(Vector2f Pos, Vector2f Goal, bool newStart = true)
    {
        Place(Pos);
        goal = Goal;
        if (newStart)
            RandomizeForces();
        Alive = true;
        Won = false;
        TimeTaken = LifeTime;
        return this;
    }
}
