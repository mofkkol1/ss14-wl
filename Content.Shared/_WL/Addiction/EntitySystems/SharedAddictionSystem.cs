using Content.Shared.Alert;
using Content.Shared._WL.Addiction.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Prototypes;
using Content.Shared.Rejuvenate;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._WL.Addiction.EntitySystems;

[UsedImplicitly]
public abstract class SharedAddictionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AddictionComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        SubscribeLocalEvent<AddictionComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AddictionComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<AddictionComponent, ComponentStartup>(OnComponentStartup);
    }

    private void OnComponentStartup(EntityUid uid, AddictionComponent component, ComponentStartup args)
    {
        UpdateEffects(uid, component);
    }

    protected virtual void OnMapInit(EntityUid uid, AddictionComponent component, MapInitEvent args)
    {
        component.LastSatisfiedTime = _timing.CurTime;
        component.NextUpdateTime = _timing.CurTime + component.UpdateRate;
        component.NextEmoteTime = _timing.CurTime;
        component.CurrentState = AddictionState.Satisfied;
        component.LastState = AddictionState.Satisfied;

        UpdateEffects(uid, component);

        DirtyFields(uid, component, null,
            nameof(AddictionComponent.LastSatisfiedTime),
            nameof(AddictionComponent.NextUpdateTime),
            nameof(AddictionComponent.NextEmoteTime),
            nameof(AddictionComponent.CurrentState),
            nameof(AddictionComponent.LastState));
    }

    private void OnRefreshMovespeed(EntityUid uid, AddictionComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (!_prototype.TryIndex(component.AddictionType, out var addictionProto))
            return;

        if (component.CurrentState == AddictionState.Withdrawal)
        {
            var modifier = addictionProto.WithdrawalSpeedModifier;
            args.ModifySpeed(modifier, modifier);
            component.SpeedModifierApplied = true;
        }
        else
        {
            component.SpeedModifierApplied = false;
        }
    }

    private void OnRejuvenate(EntityUid uid, AddictionComponent component, RejuvenateEvent args)
    {
        SatisfyAddiction(uid, component);
    }

    /// <summary>
    /// Удовлетворяет зависимость, обновляя время последнего удовлетворения и текущее состояние.
    /// </summary>
    public void SatisfyAddiction(EntityUid uid, AddictionComponent component)
    {
        var oldState = component.CurrentState;

        component.LastSatisfiedTime = _timing.CurTime;

        component.CurrentState = AddictionState.Satisfied;

        if (oldState != component.CurrentState)
        {
            UpdateEffects(uid, component);
        }

        DirtyFields(uid, component, null,
            nameof(AddictionComponent.LastSatisfiedTime),
            nameof(AddictionComponent.CurrentState));
    }

    /// <summary>
    /// Проверяет, может ли данный реагент удовлетворить зависимость.
    /// </summary>
    public bool CanSatisfyAddiction(AddictionComponent component, ReagentId reagentId)
    {
        if (!_prototype.TryIndex(component.AddictionType, out var addictionProto))
            return false;

        return addictionProto.SatisfyingReagents.Contains(reagentId.ToString());
    }

    /// <summary>
    /// Обновляет состояние зависимости на основе прошедшего времени.
    /// </summary>
    public void UpdateAddictionState(EntityUid uid, AddictionComponent component)
    {
        if (!_prototype.TryIndex(component.AddictionType, out var addictionProto))
            return;

        var oldState = component.CurrentState;
        var timeSinceLastSatisfied = _timing.CurTime - component.LastSatisfiedTime;

        var satisfactionBuffer = TimeSpan.FromSeconds(1);

        AddictionState newState;
        if (timeSinceLastSatisfied >= addictionProto.WithdrawalTime + satisfactionBuffer)
            newState = AddictionState.Withdrawal;
        else if (timeSinceLastSatisfied >= addictionProto.CravingTime + satisfactionBuffer)
            newState = AddictionState.Craving;
        else
            newState = AddictionState.Satisfied;

        component.CurrentState = newState;

        // Обновляем эффекты только если состояние изменилось
        if (oldState != component.CurrentState)
        {
            UpdateEffects(uid, component);
            DirtyField(uid, component, nameof(AddictionComponent.CurrentState));
        }
    }

    private void UpdateEffects(EntityUid uid, AddictionComponent component)
    {
        if (!_prototype.TryIndex(component.AddictionType, out var addictionProto))
            return;

        // Обновление модификатора скорости
        if (IsMovementAffectingState(component.LastState) != IsMovementAffectingState(component.CurrentState) &&
            TryComp(uid, out MovementSpeedModifierComponent? movementComponent))
        {
            _movement.RefreshMovementSpeedModifiers(uid, movementComponent);
        }

        // Очищаем все алерты перед установкой новых
        _alerts.ClearAlert(uid, addictionProto.CravingAlert);
        _alerts.ClearAlert(uid, addictionProto.WithdrawalAlert);

        // Обновление алертов
        switch (component.CurrentState)
        {
            case AddictionState.Craving:
                _alerts.ShowAlert(uid, addictionProto.CravingAlert);
                break;
            case AddictionState.Withdrawal:
                _alerts.ShowAlert(uid, addictionProto.WithdrawalAlert);
                break;
            case AddictionState.Satisfied:
                break;
        }

        component.LastState = component.CurrentState;
        DirtyField(uid, component, nameof(AddictionComponent.LastState));
    }

    private bool IsMovementAffectingState(AddictionState state)
    {
        return state == AddictionState.Withdrawal;
    }
}
