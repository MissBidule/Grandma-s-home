# Linework Lite

## About Linework Lite

Linework Lite is a free Unity package that allows you to render high-quality outlines and fill effects.

For the full documentation that is always up to date: see [https://linework.ameye.dev/free-outline/](https://linework.ameye.dev/free-outline/).

## Getting Started

### Installation

After installing/importing the asset into Unity, you can check for compatibility issues between Linework and your project by opening the compatibility check window (*Window* > *Linework Lite* > *Compatibility*). Click on the *Check Compatibility* button and see if all checkmarks are green (pass) or white (informational). If not, you can click on any of them to see an explanation.

If the result is showing only green checkmarks or white messages, you are good to go! If not, see the [Troubleshooting and Known Limitations](https://linework.ameye.dev/known-limitations/) section or contact me if you have additional questions.

### Adding outlines

Outlines in Linework Lite are rendered using renderer features. Renderer features are the way to add render effects in projects using the Universal Render Pipeline. To add an outline, open the *Universal Renderer Data* asset of your project, click on *Add Renderer Feature*, and select the *Free Outline* renderer feature.

The outline renderer feature stores its settings in a separate object that you can create somewhere in your Assets folder, by right-clicking and selecting *Create > Linework Lite > Free Outline Settings*.

Drag the created settings into the object slot of the *Free Outline* renderer feature. You can now click the *Open* button to open the settings. By default, a newly created settings object will have a single outline added to it that is applied to the whole scene. If not, you can click the *Add Outline* button to add a new outline and select which rendering layer it should target.

### Rendering Layers

Linework Lite outlines work using [Rendering Layers](https://linework.ameye.dev/outline-layers/) to control which objects should receive an outline. Read the linked documentation for more information.

### Updating the package

To update Linework, see [https://docs.unity3d.com/Manual/upm-ui-update2.html](https://docs.unity3d.com/Manual/upm-ui-update2.html).


### Removing the package

To remove Linework, see [https://docs.unity3d.com/Manual/upm-ui-remove.html](https://docs.unity3d.com/Manual/upm-ui-remove.html).

## Contact

Need any help?

[Discord](https://discord.gg/cFfQGzQdPn) • [https://ameye.dev](https://ameye.dev)
