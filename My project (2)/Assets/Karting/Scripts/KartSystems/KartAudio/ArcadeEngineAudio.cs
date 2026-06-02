using System.Collections.Generic;
using UnityEngine;

namespace KartGame.KartSystems
{
    /// <summary>
    /// Replaces the stock kart audio with a multi-layer engine bank driven by speed and throttle.
    /// Uses free loop samples copied into Resources/FurbGpAudio/Sfx/VnsCar.
    /// </summary>
    public class ArcadeEngineAudio : MonoBehaviour
    {
        private readonly struct EngineSampleSpec
        {
            public EngineSampleSpec(string resourcePath, int referenceRpm)
            {
                ResourcePath = resourcePath;
                ReferenceRpm = referenceRpm;
            }

            public string ResourcePath { get; }
            public int ReferenceRpm { get; }
        }

        private sealed class RuntimeLayer
        {
            public RuntimeLayer(AudioSource source, AudioLowPassFilter lowPass, int referenceRpm)
            {
                Source = source;
                LowPass = lowPass;
                ReferenceRpm = Mathf.Max(1, referenceRpm);
            }

            public AudioSource Source { get; }
            public AudioLowPassFilter LowPass { get; }
            public int ReferenceRpm { get; }
        }

        private static readonly EngineSampleSpec[] k_AccelerationBank =
        {
            new("FurbGpAudio/Sfx/VnsCar/1000a", 1000),
            new("FurbGpAudio/Sfx/VnsCar/2000a", 2000),
            new("FurbGpAudio/Sfx/VnsCar/3000a", 3000),
            new("FurbGpAudio/Sfx/VnsCar/4000a", 4000),
            new("FurbGpAudio/Sfx/VnsCar/5000a", 5000),
            new("FurbGpAudio/Sfx/VnsCar/6000a", 6000)
        };

        private static readonly EngineSampleSpec[] k_DecelerationBank =
        {
            new("FurbGpAudio/Sfx/VnsCar/1000a", 1000),
            new("FurbGpAudio/Sfx/VnsCar/2000d", 2000),
            new("FurbGpAudio/Sfx/VnsCar/3000d", 3000),
            new("FurbGpAudio/Sfx/VnsCar/4000d", 4000),
            new("FurbGpAudio/Sfx/VnsCar/5000d", 5000),
            new("FurbGpAudio/Sfx/VnsCar/6000d", 6000)
        };

        private const float k_DefaultEngineMasterVolume = 0.9f;
        private const float k_DefaultEngineAccelerationMaxVolume = 0.78f;
        private const float k_DefaultEngineDecelerationMaxVolume = 0.52f;
        private const float k_DefaultEngineIdleRpm = 1000f;
        private const float k_DefaultEngineMaxRpm = 6200f;
        private const float k_DefaultEngineThrottleThreshold = 0.08f;
        private const float k_DefaultEngineRpmCurvePower = 1.85f;
        private const float k_DefaultEngineMinPitch = 0.9f;
        private const float k_DefaultEngineMaxPitch = 1.015f;
        private const float k_DefaultEngineRpmResponse = 5.2f;
        private const float k_DefaultEngineThrottleResponse = 8f;
        private const float k_DefaultEngineMixResponse = 10f;
        private const float k_DefaultEngineTopEndStartRpm = 5200f;
        private const float k_DefaultEngineTopEndVolumeReduction = 0.42f;
        private const float k_DefaultEngineTopEndPitchReduction = 0.075f;
        private const float k_DefaultEngineBaseLowPassCutoff = 18000f;
        private const float k_DefaultEngineTopEndLowPassCutoff = 7200f;
        private const float k_DefaultImpactMinSpeed = 5.5f;
        private const float k_DefaultImpactMaxSpeed = 20f;
        private const float k_DefaultImpactMaxVolume = 0.58f;
        private const float k_DefaultImpactMinPitch = 0.82f;
        private const float k_DefaultImpactMaxPitch = 1.04f;

        [Header("Engine Volume")]
        [Range(0f, 1f)] public float EngineMasterVolume = 0.9f;
        [Range(0f, 1f)] public float EngineAccelerationMaxVolume = 0.78f;
        [Range(0f, 1f)] public float EngineDecelerationMaxVolume = 0.52f;

        [Header("Engine RPM")]
        public float EngineIdleRpm = 1000f;
        public float EngineMaxRpm = 6200f;
        public float EngineThrottleThreshold = 0.08f;
        public float EngineRpmCurvePower = 1.85f;
        public float EngineMinPitch = 0.9f;
        public float EngineMaxPitch = 1.015f;

        [Header("Engine Response")]
        public float EngineRpmResponse = 5.2f;
        public float EngineThrottleResponse = 8f;
        public float EngineMixResponse = 10f;

        [Header("Top-End Taming")]
        public float EngineTopEndStartRpm = 5200f;
        [Range(0f, 1f)] public float EngineTopEndVolumeReduction = 0.42f;
        public float EngineTopEndPitchReduction = 0.075f;
        public float EngineBaseLowPassCutoff = 18000f;
        public float EngineTopEndLowPassCutoff = 7200f;

        [Header("Prefab Audio Sources")]
        public AudioSource StartSound;
        public AudioSource IdleSound;
        public AudioSource RunningSound;
        public AudioSource Drift;
        public AudioSource ReverseSound;
        public float RunningSoundMaxVolume = 1f;
        public float RunningSoundMaxPitch = 1f;
        public float ReverseSoundMaxVolume = 0.5f;
        public float ReverseSoundMaxPitch = 0.6f;

        [Header("Impact")]
        public AudioClip ImpactClip;
        public string ImpactResourcePath = "FurbGpAudio/Sfx/impact_cc0";
        public float ImpactMinSpeed = 5.5f;
        public float ImpactMaxSpeed = 20f;
        [Range(0f, 1f)] public float ImpactMaxVolume = 0.58f;
        public float ImpactMinPitch = 0.82f;
        public float ImpactMaxPitch = 1.04f;

        private ArcadeKart arcadeKart;
        private readonly List<RuntimeLayer> acceleratingLayers = new();
        private readonly List<RuntimeLayer> deceleratingLayers = new();
        private AudioSource impactSource;
        private AudioSource sourceTemplate;
        private AudioSource highSpeedSource;
        private AudioSource coastSource;
        private bool useLegacySources;
        private float smoothedRpm;
        private float smoothedThrottleBlend;

        private void Awake()
        {
            ApplyRuntimeDefaults();
            arcadeKart = ResolveArcadeKart();
            ResolveLegacySources();
            sourceTemplate = RunningSound != null ? RunningSound : (IdleSound != null ? IdleSound : GetComponentInChildren<AudioSource>(true));

            if (CanUseLegacySources())
            {
                BuildLegacySources();
                useLegacySources = true;
            }
            else
            {
                BuildAudioBanks();
            }

            if (arcadeKart == null)
            {
                Debug.LogWarning("ArcadeEngineAudio did not find an ArcadeKart in this prefab hierarchy.", this);
            }

            if (!useLegacySources && acceleratingLayers.Count == 0 && deceleratingLayers.Count == 0)
            {
                Debug.LogWarning("ArcadeEngineAudio did not build any runtime audio layers.", this);
            }
        }

        private void OnDisable()
        {
            StopLegacySources();
            StopBank(acceleratingLayers);
            StopBank(deceleratingLayers);
            if (impactSource != null && impactSource.isPlaying)
            {
                impactSource.Stop();
            }
        }

        private void Update()
        {
            if (arcadeKart == null)
            {
                arcadeKart = ResolveArcadeKart();
            }

            if (arcadeKart == null)
            {
                return;
            }

            if (useLegacySources)
            {
                UpdateLegacyEngineAudio();
                return;
            }

            if (acceleratingLayers.Count == 0 && deceleratingLayers.Count == 0)
            {
                return;
            }

            var rpmBlend = EvaluateBlend(EngineRpmResponse);
            var throttleBlend = EvaluateBlend(EngineThrottleResponse);
            var targetRpm = EvaluateTargetRpm();
            var targetThrottleBlend = EvaluateThrottleBlend();

            smoothedRpm = Mathf.Lerp(smoothedRpm, targetRpm, rpmBlend);
            smoothedThrottleBlend = Mathf.Lerp(smoothedThrottleBlend, targetThrottleBlend, throttleBlend);

            var speedActivity = Mathf.InverseLerp(1.2f, 8f, arcadeKart.Rigidbody.linearVelocity.magnitude);
            var motionActivity = Mathf.Max(speedActivity, smoothedThrottleBlend);

            var accelMaster = EngineMasterVolume * EngineAccelerationMaxVolume * motionActivity * smoothedThrottleBlend;
            var decelMaster = EngineMasterVolume * EngineDecelerationMaxVolume * speedActivity * (1f - smoothedThrottleBlend);

            EnsureBankIsPlaying(acceleratingLayers);
            EnsureBankIsPlaying(deceleratingLayers);
            ApplyBank(acceleratingLayers, smoothedRpm, accelMaster, smoothedThrottleBlend);
            ApplyBank(deceleratingLayers, smoothedRpm, decelMaster, 0f);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (impactSource == null || impactSource.clip == null)
            {
                return;
            }

            var relativeSpeed = collision.relativeVelocity.magnitude;
            if (relativeSpeed < ImpactMinSpeed)
            {
                return;
            }

            var impactBlend = Mathf.InverseLerp(ImpactMinSpeed, ImpactMaxSpeed, relativeSpeed);
            var volume = Mathf.Lerp(0.08f, ImpactMaxVolume, impactBlend);
            var pitch = Mathf.Lerp(ImpactMaxPitch, ImpactMinPitch, impactBlend);

            impactSource.pitch = pitch;
            impactSource.PlayOneShot(impactSource.clip, volume);
        }

        private void BuildAudioBanks()
        {
            ClearBank(acceleratingLayers);
            ClearBank(deceleratingLayers);

            CreateBank("Accel", k_AccelerationBank, acceleratingLayers);
            CreateBank("Decel", k_DecelerationBank, deceleratingLayers);

            impactSource ??= CreateOneShotSource("ImpactAudio", 24);
            if (ImpactClip == null)
            {
                ImpactClip = Resources.Load<AudioClip>(ImpactResourcePath);
            }
            impactSource.clip = ImpactClip;
            impactSource.volume = 1f;
            impactSource.pitch = 1f;

            smoothedRpm = EngineIdleRpm;
            smoothedThrottleBlend = 0f;
        }

        private void BuildLegacySources()
        {
            ConfigureLegacyLoop(IdleSound, "FurbGpAudio/Sfx/VnsCar/1000a", true);
            ConfigureLegacyLoop(RunningSound, "FurbGpAudio/Sfx/VnsCar/3000a", true);
            ConfigureLegacyLoop(ReverseSound, "FurbGpAudio/Sfx/VnsCar/3000d", true);

            highSpeedSource = CreateLegacyClone("EngineHigh", RunningSound, "FurbGpAudio/Sfx/VnsCar/6000a");
            coastSource = ReverseSound;

            impactSource ??= CreateOneShotSource("ImpactAudio", 24);
            if (ImpactClip == null)
            {
                ImpactClip = Resources.Load<AudioClip>(ImpactResourcePath);
            }
            impactSource.clip = ImpactClip;
            impactSource.volume = 1f;
            impactSource.pitch = 1f;

            smoothedRpm = EngineIdleRpm;
            smoothedThrottleBlend = 0f;
        }

        private void ApplyRuntimeDefaults()
        {
            if (EngineMasterVolume <= 0f) EngineMasterVolume = k_DefaultEngineMasterVolume;
            if (EngineAccelerationMaxVolume <= 0f) EngineAccelerationMaxVolume = k_DefaultEngineAccelerationMaxVolume;
            if (EngineDecelerationMaxVolume <= 0f) EngineDecelerationMaxVolume = k_DefaultEngineDecelerationMaxVolume;
            if (EngineIdleRpm <= 0f) EngineIdleRpm = k_DefaultEngineIdleRpm;
            if (EngineMaxRpm <= EngineIdleRpm) EngineMaxRpm = k_DefaultEngineMaxRpm;
            if (EngineThrottleThreshold <= 0f) EngineThrottleThreshold = k_DefaultEngineThrottleThreshold;
            if (EngineRpmCurvePower <= 0f) EngineRpmCurvePower = k_DefaultEngineRpmCurvePower;
            if (EngineMinPitch <= 0f) EngineMinPitch = k_DefaultEngineMinPitch;
            if (EngineMaxPitch <= EngineMinPitch) EngineMaxPitch = k_DefaultEngineMaxPitch;
            if (EngineRpmResponse <= 0f) EngineRpmResponse = k_DefaultEngineRpmResponse;
            if (EngineThrottleResponse <= 0f) EngineThrottleResponse = k_DefaultEngineThrottleResponse;
            if (EngineMixResponse <= 0f) EngineMixResponse = k_DefaultEngineMixResponse;
            if (EngineTopEndStartRpm <= 0f) EngineTopEndStartRpm = k_DefaultEngineTopEndStartRpm;
            if (EngineTopEndVolumeReduction <= 0f) EngineTopEndVolumeReduction = k_DefaultEngineTopEndVolumeReduction;
            if (EngineTopEndPitchReduction <= 0f) EngineTopEndPitchReduction = k_DefaultEngineTopEndPitchReduction;
            if (EngineBaseLowPassCutoff <= 0f) EngineBaseLowPassCutoff = k_DefaultEngineBaseLowPassCutoff;
            if (EngineTopEndLowPassCutoff <= 0f) EngineTopEndLowPassCutoff = k_DefaultEngineTopEndLowPassCutoff;
            if (ImpactMinSpeed <= 0f) ImpactMinSpeed = k_DefaultImpactMinSpeed;
            if (ImpactMaxSpeed <= ImpactMinSpeed) ImpactMaxSpeed = k_DefaultImpactMaxSpeed;
            if (ImpactMaxVolume <= 0f) ImpactMaxVolume = k_DefaultImpactMaxVolume;
            if (ImpactMinPitch <= 0f) ImpactMinPitch = k_DefaultImpactMinPitch;
            if (ImpactMaxPitch <= 0f) ImpactMaxPitch = k_DefaultImpactMaxPitch;
            if (string.IsNullOrWhiteSpace(ImpactResourcePath)) ImpactResourcePath = "FurbGpAudio/Sfx/impact_cc0";
        }

        private void ResolveLegacySources()
        {
            IdleSound ??= FindNamedSource("engine-idle");
            RunningSound ??= FindNamedSource("engine-running");
            ReverseSound ??= FindNamedSource("engine-reverse");
            Drift ??= FindNamedSource("Drift");
            StartSound ??= FindNamedSource("Start");
        }

        private bool CanUseLegacySources()
        {
            return IdleSound != null || RunningSound != null || ReverseSound != null;
        }

        private ArcadeKart ResolveArcadeKart()
        {
            var resolvedKart = GetComponent<ArcadeKart>();
            if (resolvedKart != null) return resolvedKart;

            resolvedKart = GetComponentInParent<ArcadeKart>();
            if (resolvedKart != null) return resolvedKart;

            resolvedKart = GetComponentInChildren<ArcadeKart>(true);
            if (resolvedKart != null) return resolvedKart;

            if (transform.root != null)
            {
                resolvedKart = transform.root.GetComponentInChildren<ArcadeKart>(true);
                if (resolvedKart != null) return resolvedKart;
            }

            return FindObjectOfType<ArcadeKart>();
        }

        private void CreateBank(string prefix, IEnumerable<EngineSampleSpec> specs, List<RuntimeLayer> layers)
        {
            foreach (var spec in specs)
            {
                var clip = Resources.Load<AudioClip>(spec.ResourcePath);
                if (clip == null)
                {
                    Debug.LogWarning($"Kart audio did not find clip at '{spec.ResourcePath}'.", this);
                    continue;
                }

                var source = CreateLoopSource($"{prefix}_{spec.ReferenceRpm}", clip, out var lowPass);
                layers.Add(new RuntimeLayer(source, lowPass, spec.ReferenceRpm));
            }
        }

        private void ApplyBank(List<RuntimeLayer> layers, float rpm, float masterVolume, float throttleBlend)
        {
            if (layers.Count == 0)
            {
                return;
            }

            FindPair(layers, rpm, out var lowIndex, out var highIndex, out var interpolation);

            var lowWeight = lowIndex == highIndex ? 1f : Mathf.Cos(interpolation * Mathf.PI * 0.5f);
            var highWeight = lowIndex == highIndex ? 0f : Mathf.Sin(interpolation * Mathf.PI * 0.5f);
            var mixBlend = EvaluateBlend(EngineMixResponse);
            var topEndBlend = Mathf.Clamp01(Mathf.InverseLerp(EngineTopEndStartRpm, EngineMaxRpm, rpm)) * throttleBlend;
            var topEndVolumeFactor = 1f - (topEndBlend * EngineTopEndVolumeReduction);
            var topEndPitchReduction = topEndBlend * EngineTopEndPitchReduction;
            var lowPassCutoff = Mathf.Lerp(EngineBaseLowPassCutoff, EngineTopEndLowPassCutoff, topEndBlend);

            for (var index = 0; index < layers.Count; index++)
            {
                var layer = layers[index];
                var targetVolume = 0f;

                if (index == lowIndex)
                {
                    targetVolume = masterVolume * lowWeight * topEndVolumeFactor;
                }
                else if (index == highIndex)
                {
                    targetVolume = masterVolume * highWeight * topEndVolumeFactor;
                }

                var pitchRatio = rpm / layer.ReferenceRpm;
                var targetPitch = Mathf.Clamp(pitchRatio - topEndPitchReduction, EngineMinPitch, EngineMaxPitch);

                layer.Source.volume = Mathf.Lerp(layer.Source.volume, targetVolume, mixBlend);
                layer.Source.pitch = Mathf.Lerp(layer.Source.pitch, targetPitch, mixBlend);
                if (layer.LowPass != null)
                {
                    layer.LowPass.cutoffFrequency = Mathf.Lerp(layer.LowPass.cutoffFrequency, lowPassCutoff, mixBlend);
                }
            }
        }

        private float EvaluateTargetRpm()
        {
            var speedRatio = Mathf.Clamp01(Mathf.Abs(arcadeKart.LocalSpeed()));
            speedRatio = Mathf.Pow(speedRatio, Mathf.Max(1f, EngineRpmCurvePower));
            return Mathf.Lerp(EngineIdleRpm, EngineMaxRpm, speedRatio);
        }

        private float EvaluateThrottleBlend()
        {
            if (arcadeKart.Input.Brake && !arcadeKart.Input.Accelerate)
            {
                return 0f;
            }

            return arcadeKart.Input.Accelerate ? 1f : 0f;
        }

        private void UpdateLegacyEngineAudio()
        {
            var speedRatio = Mathf.Clamp01(Mathf.Abs(arcadeKart.LocalSpeed()));
            var throttle = EvaluateThrottleBlend();
            var rpmBlend = EvaluateBlend(EngineRpmResponse);
            var throttleBlend = EvaluateBlend(EngineThrottleResponse);
            var targetRpm = EvaluateTargetRpm();

            smoothedRpm = Mathf.Lerp(smoothedRpm, targetRpm, rpmBlend);
            smoothedThrottleBlend = Mathf.Lerp(smoothedThrottleBlend, throttle, throttleBlend);

            var motionActivity = Mathf.Max(speedRatio, smoothedThrottleBlend);
            var idleVolume = EngineMasterVolume * Mathf.Lerp(0.28f, 0.12f, speedRatio) * motionActivity * (0.55f + ((1f - smoothedThrottleBlend) * 0.45f));
            var midVolume = EngineMasterVolume * EngineAccelerationMaxVolume * Mathf.SmoothStep(0f, 1f, speedRatio) * (0.35f + (smoothedThrottleBlend * 0.65f));
            var highVolume = EngineMasterVolume * 0.42f * Mathf.SmoothStep(0.45f, 1f, speedRatio) * smoothedThrottleBlend;
            var coastVolume = EngineMasterVolume * EngineDecelerationMaxVolume * Mathf.SmoothStep(0.2f, 0.9f, speedRatio) * (1f - smoothedThrottleBlend);

            if (speedRatio < 0.04f && smoothedThrottleBlend < EngineThrottleThreshold)
            {
                idleVolume = 0f;
                midVolume = 0f;
                highVolume = 0f;
                coastVolume = 0f;
            }

            var topEndBlend = Mathf.Clamp01(Mathf.InverseLerp(EngineTopEndStartRpm, EngineMaxRpm, smoothedRpm)) * smoothedThrottleBlend;
            highVolume *= 1f - (topEndBlend * EngineTopEndVolumeReduction);

            ApplyLegacySource(IdleSound, idleVolume, Mathf.Lerp(0.92f, 0.99f, speedRatio * 0.5f));
            ApplyLegacySource(RunningSound, midVolume, Mathf.Lerp(0.94f, RunningSoundMaxPitch, speedRatio * 0.85f));
            ApplyLegacySource(highSpeedSource, highVolume, Mathf.Lerp(0.97f, 1.01f - (topEndBlend * EngineTopEndPitchReduction), speedRatio));
            ApplyLegacySource(coastSource, coastVolume, Mathf.Lerp(0.92f, ReverseSoundMaxPitch, 0.35f + (speedRatio * 0.4f)));
        }

        private static void EnsureBankIsPlaying(List<RuntimeLayer> layers)
        {
            for (var index = 0; index < layers.Count; index++)
            {
                var source = layers[index]?.Source;
                if (source != null && source.clip != null && !source.isPlaying)
                {
                    source.Play();
                }
            }
        }

        private static void FindPair(List<RuntimeLayer> layers, float rpm, out int lowIndex, out int highIndex, out float interpolation)
        {
            if (rpm <= layers[0].ReferenceRpm)
            {
                lowIndex = 0;
                highIndex = 0;
                interpolation = 0f;
                return;
            }

            var lastIndex = layers.Count - 1;
            if (rpm >= layers[lastIndex].ReferenceRpm)
            {
                lowIndex = lastIndex;
                highIndex = lastIndex;
                interpolation = 0f;
                return;
            }

            for (var index = 0; index < lastIndex; index++)
            {
                var lowRpm = layers[index].ReferenceRpm;
                var highRpm = layers[index + 1].ReferenceRpm;
                if (rpm >= lowRpm && rpm <= highRpm)
                {
                    lowIndex = index;
                    highIndex = index + 1;
                    interpolation = Mathf.InverseLerp(lowRpm, highRpm, rpm);
                    return;
                }
            }

            lowIndex = lastIndex;
            highIndex = lastIndex;
            interpolation = 0f;
        }

        private float EvaluateBlend(float response)
        {
            return 1f - Mathf.Exp(-Mathf.Max(0.01f, response) * Time.deltaTime);
        }

        private void ClearBank(List<RuntimeLayer> layers)
        {
            for (var index = 0; index < layers.Count; index++)
            {
                var source = layers[index]?.Source;
                if (source != null)
                {
                    Destroy(source.gameObject);
                }
            }

            layers.Clear();
        }

        private static void StopBank(List<RuntimeLayer> layers)
        {
            for (var index = 0; index < layers.Count; index++)
            {
                var source = layers[index]?.Source;
                if (source != null && source.isPlaying)
                {
                    source.Stop();
                }
            }
        }

        private AudioSource CreateLoopSource(string objectName, AudioClip clip, out AudioLowPassFilter lowPass)
        {
            var child = new GameObject(objectName);
            child.transform.SetParent(transform, false);

            var source = child.AddComponent<AudioSource>();
            CopyTemplateSettings(source);
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.volume = 0f;
            source.pitch = 1f;
            source.time = clip.length > 0.05f ? Random.Range(0f, clip.length - 0.01f) : 0f;
            source.Play();

            lowPass = child.AddComponent<AudioLowPassFilter>();
            lowPass.cutoffFrequency = 22000f;
            lowPass.lowpassResonanceQ = 1f;
            return source;
        }

        private AudioSource CreateOneShotSource(string objectName, int priority)
        {
            var child = new GameObject(objectName);
            child.transform.SetParent(transform, false);

            var source = child.AddComponent<AudioSource>();
            CopyTemplateSettings(source);
            source.loop = false;
            source.playOnAwake = false;
            source.priority = priority;
            return source;
        }

        private AudioSource CreateLegacyClone(string objectName, AudioSource template, string resourcePath)
        {
            if (template == null)
            {
                return null;
            }

            var child = new GameObject(objectName);
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            CopyTemplateSettings(source);
            source.loop = true;
            source.playOnAwake = false;
            source.clip = LoadOrReuseClip(resourcePath, template.clip);
            source.volume = 0f;
            source.pitch = 1f;
            if (source.clip != null)
            {
                source.Play();
            }
            return source;
        }

        private void ConfigureLegacyLoop(AudioSource source, string resourcePath, bool play)
        {
            if (source == null)
            {
                return;
            }

            source.outputAudioMixerGroup = source.outputAudioMixerGroup;
            source.playOnAwake = false;
            source.loop = true;
            source.ignoreListenerPause = true;
            source.clip = LoadOrReuseClip(resourcePath, source.clip);
            source.volume = 0f;
            source.pitch = 1f;

            if (play && source.clip != null && !source.isPlaying)
            {
                source.Play();
            }
        }

        private AudioClip LoadOrReuseClip(string resourcePath, AudioClip fallback)
        {
            var clip = string.IsNullOrWhiteSpace(resourcePath) ? null : Resources.Load<AudioClip>(resourcePath);
            return clip != null ? clip : fallback;
        }

        private AudioSource FindNamedSource(string objectName)
        {
            var sources = GetComponentsInChildren<AudioSource>(true);
            for (var index = 0; index < sources.Length; index++)
            {
                if (sources[index] != null && sources[index].gameObject.name == objectName)
                {
                    return sources[index];
                }
            }

            return null;
        }

        private void ApplyLegacySource(AudioSource source, float volume, float pitch)
        {
            if (source == null)
            {
                return;
            }

            if (source.clip != null && !source.isPlaying)
            {
                source.Play();
            }

            var blend = EvaluateBlend(EngineMixResponse);
            source.volume = Mathf.Lerp(source.volume, Mathf.Clamp01(volume), blend);
            source.pitch = Mathf.Lerp(source.pitch, Mathf.Clamp(pitch, EngineMinPitch, 1.05f), blend);
        }

        private void StopLegacySources()
        {
            StopLegacySource(IdleSound);
            StopLegacySource(RunningSound);
            StopLegacySource(ReverseSound);
            StopLegacySource(highSpeedSource);
        }

        private static void StopLegacySource(AudioSource source)
        {
            if (source != null && source.isPlaying)
            {
                source.Stop();
            }
        }

        private void CopyTemplateSettings(AudioSource source)
        {
            if (sourceTemplate != null)
            {
                source.outputAudioMixerGroup = sourceTemplate.outputAudioMixerGroup;
                source.spatialBlend = sourceTemplate.spatialBlend;
                source.rolloffMode = sourceTemplate.rolloffMode;
                source.minDistance = sourceTemplate.minDistance;
                source.maxDistance = Mathf.Max(sourceTemplate.maxDistance, 12f);
                source.dopplerLevel = 0f;
                source.spread = sourceTemplate.spread;
                source.priority = sourceTemplate.priority;
                source.bypassEffects = sourceTemplate.bypassEffects;
                source.bypassListenerEffects = sourceTemplate.bypassListenerEffects;
                source.bypassReverbZones = sourceTemplate.bypassReverbZones;
                source.reverbZoneMix = sourceTemplate.reverbZoneMix;
                source.ignoreListenerPause = true;
                return;
            }

            source.spatialBlend = 0f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1f;
            source.maxDistance = 12f;
            source.dopplerLevel = 0f;
            source.spread = 0f;
            source.priority = 32;
            source.ignoreListenerPause = true;
        }
    }
}
