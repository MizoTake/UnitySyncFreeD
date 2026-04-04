using System.Collections;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MizoTake.SyncFreeD.Tests.Runtime
{
    public sealed class SampleVisualReferenceRigBehaviourPlayModeTests
    {
        [UnityTest]
        public IEnumerator OnEnable_CreatesVisualMarkersWithMaterials()
        {
            var gameObject = new GameObject("Sample Visual Rig Root");
            gameObject.AddComponent<SampleVisualReferenceRigBehaviour>();

            yield return null;

            Assert.That(gameObject.transform.childCount, Is.EqualTo(9));
            Assert.That(gameObject.transform.Find("Center Tower"), Is.Not.Null);
            Assert.That(gameObject.transform.Find("Near Target"), Is.Not.Null);
            Assert.That(gameObject.transform.Find("Center Line"), Is.Not.Null);
            Assert.That(gameObject.transform.Find("Far Target").GetComponent<Renderer>().sharedMaterial, Is.Not.Null);
            Assert.That(gameObject.transform.Find("Left Marker/Left Marker Label"), Is.Not.Null);
            Assert.That(gameObject.transform.Find("Depth Pole/Depth Pole Label").GetComponent<TextMesh>().text, Is.EqualTo("Depth Pole"));

            Object.Destroy(gameObject);
        }
    }
}
