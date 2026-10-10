using System;
using UnityEngine;

// Put on a [SerializeReference] field or list to pick its class from a dropdown in the
// inspector (see SubclassPickerDrawer), e.g. the steps of a StoryEvent.
public sealed class SubclassPickerAttribute : PropertyAttribute
{
}

// Where a class sits in the SubclassPicker dropdown, e.g. "Characters/Walk To".
// Without it the class name is shown.
[AttributeUsage(AttributeTargets.Class)]
public sealed class SubclassMenuAttribute : Attribute
{
    public SubclassMenuAttribute(string path)
    {
        Path = path;
    }

    public string Path { get; }
}
