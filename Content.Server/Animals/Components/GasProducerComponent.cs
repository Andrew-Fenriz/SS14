using Content.Server.Animals.Systems;
using Content.Shared.Atmos;

namespace Content.Server.Animals.Components;

/// <summary>
/// Produces configured gases into the producer's surrounding atmosphere.
/// </summary>
[RegisterComponent, Access(typeof(GasProducerSystem))]
public sealed partial class GasProducerComponent : Component
{
    /// <summary>
    /// Gases and their amounts in moles produced per successful production attempt.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<Gas, float> Gases = [];
}
