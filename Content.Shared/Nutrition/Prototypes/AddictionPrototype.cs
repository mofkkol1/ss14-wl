using Content.Shared.Alert;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;


namespace Content.Shared.Nutrition.Prototypes;

/// <summary>
/// Прототип зависимости.
/// </summary>
[Prototype("addiction")]
public sealed partial class AddictionPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Имя зависимости.
    /// </summary>
    [DataField("name")]
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Описание зависимости.
    /// </summary>
    [DataField("description")]
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Список реагентов, которые могут удовлетворить эту зависимость.
    /// </summary>
    [DataField("satisfyingReagents", required: true)]
    public HashSet<string> SatisfyingReagents = new();

    /// <summary>
    /// Время в секундах перед началом тяги.
    /// </summary>
    [DataField("cravingTime")]
    public TimeSpan CravingTime { get; private set; } = TimeSpan.FromMinutes(20);

    /// <summary>
    /// Время в секундах перед началом ломки.
    /// </summary>
    [DataField("withdrawalTime")]
    public TimeSpan WithdrawalTime { get; private set; } = TimeSpan.FromMinutes(40);

    /// <summary>
    /// Модификатор скорости во время ломки. По-умолчанию - 80% от обычной скорости.
    /// </summary>
    [DataField("withdrawalSpeedModifier")]
    public float WithdrawalSpeedModifier { get; private set; } = 0.8f;

    /// <summary>
    /// Эмоция, показываемая во время ломки.
    /// </summary>
    [DataField("withdrawalEmote")]
    public string WithdrawalEmote { get; private set; } = "Tremble";

    /// <summary>
    /// Интервал между эмоутами во время ломки в секундах.
    /// </summary>
    [DataField("withdrawalEmoteInterval")]
    public TimeSpan WithdrawalEmoteInterval { get; private set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Алерт, показываемый во время тяги.
    /// </summary>
    [DataField("cravingAlert", required: true)]
    public ProtoId<AlertPrototype> CravingAlert { get; private set; } = default!;

    /// <summary>
    /// Алерт, показываемый во время ломки.
    /// </summary>
    [DataField("withdrawalAlert", required: true)]
    public ProtoId<AlertPrototype> WithdrawalAlert { get; private set; } = default!;

    /// <summary>
    /// Минимальное количество реагента, необходимое для удовлетворения зависимости.
    /// </summary>
    [DataField("minimumSatisfyingAmount")]
    public float MinimumSatisfyingAmount { get; private set; } = 5.0f;
}
