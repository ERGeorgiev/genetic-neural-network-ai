# Genetika

A general-purpose **Genetic Algorithm + Neural Network** framework in C#. Genetika combines evolutionary algorithms with artificial neural networks to create adaptable AI systems that evolve to solve complex problems.

## Features

- **Genetic Algorithm Core** -- Population-based evolution with configurable selection, crossover, and mutation
- **Neural Networks** -- Dynamically-generated feed-forward networks with variable hidden layers and neuron counts
- **Pluggable Strategies**
  - **Selection**: Tournament, Roulette Wheel, Random
  - **Crossover**: Single-point, Multi-point, Order-based
  - **Mutation**: Exchange, Randomize Weights, Nullify Weights, Nullify Inputs
  - **Fitness Scaling**: Configurable scaling types
- **Serialization** -- Save and load genes, genomes, and parameters as JSON
- **Interactive Console** -- Step through generations, inspect genes, view neural networks, and isolate individuals
- **Practical Examples** -- 7 demonstrations including physics simulations and AI-generated art

## Getting Started

### Build

```bash
# Visual Studio
Open Genetika.sln and build the solution

# Command line
msbuild Genetika.sln
```

### Run Examples

Build the solution, then run the `Genetika.Examples` project. A console menu lets you pick a demonstration:

| Key | Demo |
|-----|------|
| V | Velocity |
| I | Inertia |
| T | Translation |
| N | Velocity with Noise |
| D | Inertia Double |
| P | Paint |

### Console Controls

Once inside a demo, use these keys:

| Key | Action |
|-----|--------|
| Enter | Run next generation |
| P | Print all genes and fitness |
| F | Focus on a specific gene |
| I | Isolate and simulate a gene |
| N | Print neural network details |
| S | Save gene/genome to JSON |
| L | Load gene/genome from JSON |
| R | Remove a gene from the population |

## Usage

Implement the `IEntity<T>` interface to define your own problem domain:

```csharp
public interface IEntity<T> : ITablePrint<T>
{
    int Id { get; }
    bool AccumulativeFitness { get; }
    FitnessLogic FitnessLogic { get; }

    float GetFitness();          // Evaluate how well this entity performs
    void Update(float[] outputs); // Apply neural network outputs
    float[] GenerateInput();      // Provide inputs to the neural network
    void Restart();               // Reset for a new evaluation
}
```

Configure evolution parameters via `GenetikaParameters`:

```csharp
var parameters = new GenetikaParameters
{
    population = 100,
    geneReplaceRatio = 0.4m,
    elitismRatio = 0.2m,
    selectionType = SelectionType.Tournament,
    crossoverType = CrossoverType.SinglePoint
};
GenetikaParameters.MutationRate = 0.1f;
```

## Paint Example

The Paint demo evolves neural networks that control a virtual brush to generate artwork. Each network receives 11 sensory inputs (position, progress, system metrics, previous color) and produces 9 outputs (movement, color, brush size, opacity, type, pressure).

Generated paintings are saved to the `paintings/` directory.
