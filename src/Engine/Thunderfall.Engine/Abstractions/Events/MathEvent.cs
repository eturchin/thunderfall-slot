using System.Text.Json.Serialization;

namespace Thunderfall.Engine.Abstractions.Events;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "eventType")]
[JsonDerivedType(typeof(BoardGenerated), "BoardGenerated")]
[JsonDerivedType(typeof(WinningSymbolsDetected), "WinningSymbolsDetected")]
[JsonDerivedType(typeof(SymbolsRemoved), "SymbolsRemoved")]
[JsonDerivedType(typeof(SymbolsDropped), "SymbolsDropped")]
[JsonDerivedType(typeof(SymbolsSpawned), "SymbolsSpawned")]
[JsonDerivedType(typeof(CascadeCompleted), "CascadeCompleted")]
[JsonDerivedType(typeof(MultiplierCollected), "MultiplierCollected")]
[JsonDerivedType(typeof(RoundWinCalculated), "RoundWinCalculated")]
[JsonDerivedType(typeof(ScatterTriggerDetected), "ScatterTriggerDetected")]
[JsonDerivedType(typeof(FreeSpinsStarted), "FreeSpinsStarted")]
[JsonDerivedType(typeof(FreeSpinCompleted), "FreeSpinCompleted")]
[JsonDerivedType(typeof(FreeSpinsRetriggered), "FreeSpinsRetriggered")]
[JsonDerivedType(typeof(BonusMultiplierUpdated), "BonusMultiplierUpdated")]
[JsonDerivedType(typeof(MaxWinReached), "MaxWinReached")]
[JsonDerivedType(typeof(FeatureCompleted), "FeatureCompleted")]
public abstract record MathEvent(int SequenceNumber)
{
    public int SchemaVersion { get; init; } = 1;
}
