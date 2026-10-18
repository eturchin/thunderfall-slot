using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Thunderfall.Benchmarks;
using Thunderfall.Engine.Abstractions.Contracts;
using Thunderfall.Engine.Abstractions.Definitions;
using Thunderfall.Engine.Evaluation;
using Thunderfall.Engine.Primitives;
using Thunderfall.Engine.Randomness;
using Thunderfall.Engine.Runtime;
using Thunderfall.Engine.Simulation.Simulation;

BenchmarkSwitcher.FromAssembly(typeof(EngineBenchmarks).Assembly).Run(args);

namespace Thunderfall.Benchmarks
{
    [MemoryDiagnoser]
    public class EngineBenchmarks
    {
        private readonly WeightedCellBoardGenerator _generator = new();
        private readonly CountAnywhereEvaluator _evaluator = new();
        private readonly GravityCascadePolicy _gravity = new();
        private RoundEngine _model = null!;
        private Xoshiro256StarStar _rng = null!;
        private Cell[] _cells = null!;
        private Cell[] _original = null!;
        private int _identity;
        private int[] _counts = null!;

        [GlobalSetup]
        public void Setup()
        {
            _model = new(
                DefinitionJson.Load(
                    Path.Combine(AppContext.BaseDirectory, "configs/thunderfall/v1/game.json")
                )
            );
            _rng = new(42);
            _cells = new Cell[30];
            new WeightedCellBoardGenerator().Generate(
                _cells,
                _model.Configuration,
                _model.Configuration.Base,
                _rng,
                ref _identity
            );
            _original = _cells.ToArray();
            _counts = new int[12];
        }

        [Benchmark]
        public Cell[] BoardGeneration()
        {
            _identity = 0;
            _generator.Generate(_cells, _model.Configuration, _model.Configuration.Base, _rng, ref _identity);
            return _cells;
        }

        [Benchmark]
        public WinEvaluation SymbolFrequency() =>
            _evaluator.Evaluate(_cells, _model.Configuration, _counts);

        [Benchmark]
        public Cell[] TumbleGravity()
        {
            _identity = 30;
            _original.CopyTo(_cells, 0);
            _gravity.Apply(_cells, 255, _model.Configuration, _model.Configuration.Base, _rng, ref _identity);
            return _cells;
        }

        [Benchmark(OperationsPerInvoke = 1000)]
        public int WeightedSampling()
        {
            var result = 0;
            for (var i = 0; i < 1000; i++)
            {
                result += _model.Configuration.Base.Initial.Sample(_rng);
            }

            return result;
        }

        [Benchmark]
        public RoundSummary FullRoundTraceOff() =>
            _model.ExecuteSummary(new(1000, Guid.Empty), GameMathState.Empty, _rng);

        [Benchmark]
        public RoundOutcome FullRoundTraceOn() =>
            _model.Execute(new(1000, Guid.Empty), GameMathState.Empty, _rng);

        [Benchmark]
        public SimulationReport MillionPaidCycles() =>
            new SimulationRunner().Run(_model, new(1_000_000, 42, Workers: 1));
    }
}
