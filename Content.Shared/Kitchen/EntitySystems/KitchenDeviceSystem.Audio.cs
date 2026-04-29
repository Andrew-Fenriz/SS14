using Robust.Shared.Audio;

namespace Content.Shared.Kitchen.EntitySystems;

public partial class KitchenDeviceSystem
{
    /// <summary>
    /// Plays a click sound when interacting with the device UI.
    /// </summary>
    public void PlayClickSound(EntityUid uid, SoundSpecifier clickSound, EntityUid? actor)
    {
        _audio.PlayPredicted(clickSound, uid, actor);
    }

    /// <summary>
    /// Plays the sound when the device starts operating.
    /// </summary>
    public void PlayStartSound(EntityUid uid, SoundSpecifier startSound)
    {
        _audio.PlayPvs(startSound, uid);
    }

    /// <summary>
    /// Plays the sound when the device finishes operating.
    /// </summary>
    public void PlayDoneSound(EntityUid uid, SoundSpecifier doneSound)
    {
        _audio.PlayPvs(doneSound, uid);
    }

    /// <summary>
    /// Starts a looping sound (e.g., cooking/grinding noise).
    /// </summary>
    public void StartLoopingSound(EntityUid uid, SoundSpecifier loopingSound, ref EntityUid? audioStream, float pitchMultiplier = 1f)
    {
        audioStream = _audio.PlayPvs(loopingSound, uid,
            AudioParams.Default.WithLoop(true).WithMaxDistance(5)
                .WithPitchScale(1 / pitchMultiplier))?.Entity;
    }

    /// <summary>
    /// Stops the currently playing looping sound.
    /// </summary>
    public void StopLoopingSound(ref EntityUid? audioStream)
    {
        audioStream = _audio.Stop(audioStream);
    }

    /// <summary>
    /// Plays a one-off sound with optional audio parameters.
    /// </summary>
    public void PlaySound(EntityUid uid, SoundSpecifier sound, AudioParams? audioParams = null)
    {
        _audio.PlayPvs(sound, uid, audioParams ?? AudioParams.Default);
    }

    /// <summary>
    /// Starts a looping sound based on the current operating mode.
    /// </summary>
    public void StartModeLoopingSound(EntityUid uid, Dictionary<string, SoundSpecifier> modeSounds, string mode, ref EntityUid? audioStream, float pitchMultiplier = 1f)
    {
        if (!modeSounds.TryGetValue(mode, out var sound))
            return;

        audioStream = _audio.PlayPvs(sound, uid,
            AudioParams.Default.WithLoop(true).WithMaxDistance(5)
                .WithPitchScale(1 / pitchMultiplier))?.Entity;
    }
}
