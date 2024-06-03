#nullable enable

using System;
using UnityEngine;
using UnityEngine.Search;
using UnityEngine.Playables;
using UnityEngine.Timeline;

using Cysharp.Threading.Tasks;
using UniRx;

namespace Hedwig.RTSCore.Model
{
    [CreateAssetMenu(menuName = "Hedwig/Effect/Hit", fileName = "HitEffect")]
    public class HitEffect : ScriptableObject, IHitEffectFactory
    {
        [SerializeReference, SubclassSelector]
        IHitEffectFactory Factory;

        public IEffect Create(ITransformProvider parent, Vector3 position, Vector3 direction)
            => Factory.Create(parent, position, direction);
    }

    public interface IHitEffectFactory
    {
        IEffect Create(ITransformProvider parent, Vector3 position, Vector3 direction);
    }

    [Serializable]
    public class DelayHitEffect : IHitEffectFactory
    {
        [SerializeField]
        int msec = 0;

        class DelayEffectImpl : IEffect
        {
            public int msec;
            UniTask IEffect.Play()
            {
                return UniTask.Delay(msec);
            }
            void IDisposable.Dispose()
            { }
        }

        IEffect IHitEffectFactory.Create(ITransformProvider parent, Vector3 position, Vector3 direction)
        {
            return new DelayEffectImpl() { msec = msec };
        }
    }

    [Serializable]
    public class PrefabHitEffect : IHitEffectFactory
    {
        [SerializeField, SearchContext("t:prefab effect")]
        [SerializableRequired]
        public GameObject? prefab;

        IEffect IHitEffectFactory.Create(ITransformProvider parent, Vector3 position, Vector3 direction)
        {
            if (!GameObject.Instantiate(prefab!).TryGetComponent<IHitEffect>(out var effect))
            {
                throw new InvalidConditionException("No IHitEffect");
            }
            effect.Initialize(parent, position, direction);
            return effect;
        }
    }

    [Serializable]
    public class TimelineEffect : IHitEffectFactory
    {
        [SerializeField, SearchContext("t:TimelineAsset")]
        [SerializableRequired]
        public TimelineAsset? _timeline;

        class PlayableDirectorEffect : IEffect
        {
            PlayableDirector _director;
            public PlayableDirectorEffect(PlayableDirector director)
            {
                _director = director;
            }
            async UniTask IEffect.Play()
            {
                var source = new UniTaskCompletionSource();
                var disposed = Observable.FromEvent<PlayableDirector>(h => _director.stopped += h, h => _director.stopped -= h)
                    .Subscribe(_ =>
                    {
                        source.TrySetResult();
                    });
                _director.Play();
                await source.Task;
                disposed.Dispose();
            }
            public void Dispose() { }
        }

        IEffect IHitEffectFactory.Create(ITransformProvider parent, Vector3 position, Vector3 direction)
        {
            var go = parent.Transform.Raw.gameObject;
            var director = go.AddComponent<PlayableDirector>();
            // Debug.Break();

            foreach(var track in _timeline!.GetOutputTracks()) {
                if(track is ControlTrack) {
                    foreach (var clip in track.GetClips())
                    {
                        if (clip.asset is ControlPlayableAsset controlPlayableAsset)
                        {
                            // controlPlayableAsset.prefabGameObject = go;
                            controlPlayableAsset.sourceGameObject.exposedName = UnityEditor.GUID.Generate().ToString();
                            // controlPlayableAsset.sourceGameObject.exposedName = UnityEditor.GUID.Generate().ToString();
                            director.SetReferenceValue(controlPlayableAsset.sourceGameObject.exposedName, go);

                            // ここで、ParentObjectを設定
                            controlPlayableAsset.updateDirector = true;
                        }
                    }
                }
            }

            director.playableAsset = _timeline!;
            return new PlayableDirectorEffect(director);
        }
    }
}