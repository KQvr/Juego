using System;
using UnityEngine;

public enum Handedness { Right = 0, Left = 1 }

public enum HandRole { Dominant, NonDominant }

/// <summary>
/// Guarda la preferencia de mano dominante del usuario.
/// Cualquier script puede suscribirse a OnChanged para actualizarse cuando
/// el usuario cambie la mano en el menu de opciones.
/// </summary>
public static class HandPreference
{
    private const string KEY = "DominantHand";

    public static event Action<Handedness> OnChanged;

    public static Handedness Dominant
    {
        get => (Handedness)PlayerPrefs.GetInt(KEY, (int)Handedness.Right);
        set
        {
            if (Dominant == value) return;
            PlayerPrefs.SetInt(KEY, (int)value);
            PlayerPrefs.Save();
            OnChanged?.Invoke(value);
        }
    }

    public static Handedness NonDominant =>
        Dominant == Handedness.Right ? Handedness.Left : Handedness.Right;
}