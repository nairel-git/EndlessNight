using System.Collections.Generic;
using System.Linq;

public class Cell
{
    public bool IsCollapsed { get; private set; }
    public Chunk FinalModule { get; private set; }
    public List<Chunk> PossibleModules { get; private set; }
    
    private float tieBreakerNoise;

    public Cell(List<Chunk> allModules, float noise)
    {
        IsCollapsed = false;
        PossibleModules = new List<Chunk>(allModules);
        tieBreakerNoise = noise;
    }

    public float Entropy
    {
        get
        {
            if (IsCollapsed) return float.MaxValue;
            return PossibleModules.Sum(m => m.Weight) + tieBreakerNoise;
        }
    }

    public void Collapse(System.Random prng)
    {
        if (PossibleModules.Count == 0) return;

        float totalWeight = PossibleModules.Sum(m => m.Weight);
        float randomPoint = (float)prng.NextDouble() * totalWeight;
        float cumulative = 0f;

        foreach (var module in PossibleModules)
        {
            cumulative += module.Weight;
            if (randomPoint <= cumulative)
            {
                FinalModule = module;
                break;
            }
        }

        if (FinalModule == null) FinalModule = PossibleModules[0];

        PossibleModules = new List<Chunk> { FinalModule };
        IsCollapsed = true;
    }

    public void ForceCollapse(Chunk module)
    {
        FinalModule = module;
        PossibleModules = new List<Chunk> { module };
        IsCollapsed = true;
    }

    // Used by the backtracking system to make a deep copy of the cell's current state
    public Cell Clone()
    {
        Cell clone = new Cell(this.PossibleModules, this.tieBreakerNoise);
        clone.IsCollapsed = this.IsCollapsed;
        clone.FinalModule = this.FinalModule;
        return clone;
    }
}