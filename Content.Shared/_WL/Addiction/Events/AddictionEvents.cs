using Content.Shared.Chemistry.Components;

namespace Content.Shared._WL.Addiction.Events;

/// <summary>
/// Проверяет, может ли данный реагент удовлетворить зависимость.
/// </summary>
/// <param name="Solution">Раствор.</param>
/// <param name="Amount">Количество реагента, которое было выпито.</param>
[ByRefEvent]
public record struct CheckAddictionSatisfactionEvent(Solution Solution, float Amount);

/// <summary>
/// Уведомляет, что зависимость была удовлетворена.
/// </summary>
/// <param name="AddictionType">Тип зависимости.</param>
/// <param name="User">Сущность, у которой была удовлетворена зависимость.</param>
[ByRefEvent]
public record struct AddictionSatisfiedEvent(string AddictionType, EntityUid User);
