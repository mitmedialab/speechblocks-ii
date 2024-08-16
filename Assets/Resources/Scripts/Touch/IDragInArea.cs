using UnityEngine;

// area for dragging stuff into, like the flip control on the main canvas
// inherits tappable, so that it can describe what it is to the user when tapped
public interface IDragInArea : ITappable
{
    bool AcceptDragIn(GameObject gameObject);
}
