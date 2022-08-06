using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Search;
using UnityEngine.UI;

using Cysharp.Threading.Tasks;
using UniRx;
using VContainer;
using VContainer.Unity;

namespace Hedwig.RTSCore.Teset
{
    public class EffectTest : LifetimeScope
    {
        [SerializeField]
        List<GameObject> targets = new List<GameObject>();

        [SerializeField]
        Button hitButton;

        [SerializeField]
        Toggle continuousToggle;

        [SerializeField, SearchContext("t:prefab effect")]
        GameObject damageEffectPrefab;

        [SerializeField, SearchContext("t:prefab effect")]
        GameObject hitEffectPrefab;

        protected override void Configure(IContainerBuilder builder)
        {
            foreach (var target in targets)
            {
                var mr = target.transform.gameObject.GetComponent<MeshRenderer>();
                mr.material.color = new Color(0.1f, 0.1f, 0.1f);
            }
        }

        IEffect createDamageEffect(ITransformProvider parent, DamageEffectParameter duration, int damage)
        {
            var effect = Instantiate(damageEffectPrefab).GetComponent<IDamageEffect>();
            effect.Initialize(parent, duration, damage);
            return effect;
        }

        IEffect createHitEffect(ITransformProvider parent, Vector3 position, Vector3 direction)
        {
            var effect = Instantiate(hitEffectPrefab).GetComponent<IHitEffect>();
            effect.Initialize(parent, position, direction);
            return effect;
        }

        IEffect[] createEffects(ITransformProvider target, int damage, Vector3 position)
        {
            return new IEffect[] {
            createDamageEffect(target, new DamageEffectParameter() { duration = 1 }, damage),
            createHitEffect(target, position, Vector3.zero)
        }.Where(e => e != null).ToArray();
        }

        Vector3 hitPosition(ITransformProvider target)
        {
            var position = Vector3.zero;
            if (target != null)
            {
                var hit = new RaycastHit();
                var origin = Camera.main.transform.position;
                var dir = target.transform.Position - origin;
                if (!Physics.Raycast(origin, dir, out hit, 100))
                {
                    Debug.LogError("failed not raycast");
                    return Vector3.zero;
                }
                return hit.point;
            }
            else
            {
                return Vector3.zero;
            }
        }

        class DummyMobileObject : MonoBehaviour, ITransformProvider
        {
            ITransform _transform = new CachedTransform();
            ITransform ITransformProvider.transform { get => _transform; }

            public void Dispose() { Destroy(gameObject); }
            void Awake()
            {
                _transform.Initialize(transform);
            }
        }

        void Start()
        {
            hitButton.OnClickAsObservable().Subscribe(async _ =>
            {
                if (!continuousToggle.isOn)
                {
                    hitButton.interactable = false;
                }

                var _targets = targets.Select(target =>
                {
                    return target.AddComponent<DummyMobileObject>() as ITransformProvider;
                });

                var tasks = new List<UniTask>();

                foreach (var target in _targets)
                {
                    var e = createEffects(target, Random.Range(1, 30), hitPosition(target));
                    tasks.Add(e.PlayAndDispose());
                }
                await UniTask.WhenAll(tasks);

                if (!continuousToggle.isOn)
                {
                    hitButton.interactable = true;
                }
            }).AddTo(this);
        }
    }
}