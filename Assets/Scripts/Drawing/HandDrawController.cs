using System;
using System.Collections.Generic;
using UnityEngine;

public class HandDrawController : MonoBehaviour
{
    [Header("Pencil")]
    [SerializeField] private PencilFollower pencilFollower;
    [SerializeField] private PencilContactDetector contactDetector;

    [Header("Rendering")]
    [SerializeField] private Material lineMaterial;
    [SerializeField] private float strokeWidth = 0.01f;
    [SerializeField] private int tubeSides = 8;

    [Header("Stroke Settings")]
    [SerializeField] private float minPointDistance = 0.008f;
    [SerializeField] private bool enableGravity = false;
    [SerializeField] private bool enableCollider = false;

    private readonly List<TubeRenderer> tubes = new();
    private TubeRenderer currentTube;
    private StrokeRecorder recorder;

    private bool wasTouching;

    void Awake()
    {
        recorder = new StrokeRecorder(minPointDistance);
        NewTube();
    }

    void Update()
    {
        if (pencilFollower == null || contactDetector == null) return;
        if (!contactDetector.IsTouching) 
        {
            EndStrokeIfNeeded();
            return;
        }

        var tip = pencilFollower.TipTransform;
        if (tip == null) return;

        if (!wasTouching)
            recorder.BeginStroke();

        if (recorder.TryAddPoint(contactDetector.ContactPoint))
        {
            currentTube.SetPositions(recorder.CurrentStroke.ToArray());
        }

        wasTouching = true;
    }

    private void EndStrokeIfNeeded()
    {
        if (!wasTouching) return;

        if (enableGravity)
            currentTube.EnableGravity();

        recorder.EndStroke();
        NewTube();
        wasTouching = false;
    }

    void NewTube()
    {
        var go = new GameObject($"StrokeTube_{tubes.Count}");
        go.transform.SetParent(transform, true);

        var tr = go.AddComponent<TubeRenderer>();
        tr.InitIfNeeded();
        tubes.Add(tr);

        var mr = go.GetComponent<MeshRenderer>();
        mr.material = lineMaterial;

        tr._radiusOne = strokeWidth;
        tr._radiusTwo = strokeWidth;
        tr._sides = tubeSides;
        tr.ColliderTrigger = enableCollider;

        tr.SetPositions(Array.Empty<Vector3>());
        currentTube = tr;
    }
}
