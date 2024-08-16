using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;

public class AnimationMaster : MonoBehaviour {
    public IAnimation StartAnimation(GameObject gameObject, Type animationType, params object[] args) {
        try
        {
            string anim_upd_type = GetUpdateType(animationType);
            IAnimation[] animations = gameObject.GetComponents<IAnimation>();
            foreach (IAnimation animation in animations)
            {
                string other_anim_upd_type = GetUpdateType(animation.GetType());
                if (other_anim_upd_type.Equals(anim_upd_type))
                {
                    animation.Stop();
                }
            }
            IAnimation new_animation = (IAnimation)gameObject.AddComponent(animationType);
            new_animation.Init(args);
            new_animation.Start();
            return new_animation;
        }
        catch (Exception e)
        {
            ExceptionUtil.OnException(e);
            return null;
        }
    }

    public Glide StartGlide(GameObject gameObject, Vector2 endPos, float duration) {
        return (Glide)StartAnimation(gameObject, typeof(Glide), endPos, duration);
    }

    public Glide StartGlide(GameObject gameObject, Vector2 endPos, float duration, Func<float, float> easing)
    {
        return (Glide)StartAnimation(gameObject, typeof(Glide), endPos, duration, easing);
    }

    public LocalGlide StartLocalGlide(GameObject gameObject, Vector2 endPos, float duration) {
        return (LocalGlide)StartAnimation(gameObject, typeof(LocalGlide), endPos, duration);
    }

    public LocalGlide StartLocalGlide(GameObject gameObject, Vector2 endPos, float duration, Func<float, float> easing)
    {
        return (LocalGlide)StartAnimation(gameObject, typeof(LocalGlide), endPos, duration);
    }

    public Fade StartFade(GameObject gameObject, float target, float duration) {
        return (Fade)StartAnimation(gameObject, typeof(Fade), target, duration);
    }

    public Scale StartScale(GameObject gameObject, Vector3 targetScale, float duration) {
        return (Scale)StartAnimation(gameObject, typeof(Scale), targetScale, duration);
    }

    public Scale StartScale(GameObject gameObject, Vector3 targetScale, float duration, Func<float, float> easing)
    {
        return (Scale)StartAnimation(gameObject, typeof(Scale), targetScale, duration, easing);
    }

    public Scale StartScale(GameObject gameObject, float targetScale, float duration)
    {
        return (Scale)StartAnimation(gameObject, typeof(Scale), new Vector3(targetScale, targetScale, targetScale), duration);
    }

    public Scale StartScale(GameObject gameObject, float targetScale, float duration, Func<float, float> easing)
    {
        return (Scale)StartAnimation(gameObject, typeof(Scale), new Vector3(targetScale, targetScale, targetScale), duration);
    }

    public Resize StartResize(GameObject gameObject, float targetW, float targetH, float duration) {
        return (Resize)StartAnimation(gameObject, typeof(Resize), targetW, targetH, duration);
    }

    public Resize StartResize(GameObject gameObject, float targetW, float targetH, float duration, Func<float, float> easing)
    {
        return (Resize)StartAnimation(gameObject, typeof(Resize), targetW, targetH, duration, easing);
    }

    public PulseOpacity StartPulseOpacity(GameObject gameObject, float min, float max, float period) {
        return (PulseOpacity)StartAnimation(gameObject, typeof(PulseOpacity), min, max, period);
    }

    public PulseSize StartPulseSize(GameObject gameObject, float max, float duration, int iterations) {
        return (PulseSize)StartAnimation(gameObject, typeof(PulseSize), max, duration, iterations);
    }

    public static string GetUpdateType(Type animationType) {
        return (string)(animationType.GetMethod("UpdateType", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy).Invoke(null, null));
    }
}