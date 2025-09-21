using Content.Shared._WL.Addiction.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._WL.Addiction.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedAddictionSystem))]
[AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
public sealed partial class AddictionComponent : Component
{
    /// <summary>
    /// Тип зависимости.
    /// </summary>
    [DataField("addictionType", required: true)]
    [AutoNetworkedField]
    public ProtoId<AddictionPrototype> AddictionType = default!;

    /// <summary>
    /// Текущее состояние зависимости.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public AddictionState CurrentState = AddictionState.Satisfied;

    /// <summary>
    /// Предыдущее состояние зависимости.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public AddictionState LastState = AddictionState.Satisfied;

    /// <summary>
    /// Время, когда зависимость была в последний раз удовлетворена.
    /// </summary>
    [DataField("lastSatisfiedTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan LastSatisfiedTime;

    /// <summary>
    /// Время, когда нужно будет в следующий раз показать эмоут.
    /// </summary>
    [DataField("nextEmoteTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan NextEmoteTime;

    /// <summary>
    /// Время между обновлениями состояния зависимости.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField, AutoNetworkedField]
    public TimeSpan UpdateRate = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Время, когда нужно будет в следующий раз обновить зависимость.
    /// </summary>
    [DataField("nextUpdateTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan NextUpdateTime;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool SpeedModifierApplied = true;
}

public enum AddictionState : byte
{
    Satisfied = 0,
    Craving = 1,
    Withdrawal = 2
}
