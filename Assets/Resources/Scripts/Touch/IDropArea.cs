using UnityEngine;

// area for dropping stuff in, like a trash bin
// inherits tappable, so that it can describe what it is to the user when tapped
public interface IDropArea : ITappable
{
    bool AcceptDrop(GameObject gameObject);
}
