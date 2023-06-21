Uxml Binding Generator
======================

A package which extends the Ui Toolkit usage by adding a binding generator for any *.uxml (VisualTreeAsset) file within your project. This will allow you to automatically create code files, which simplifies your uxml querying and avoid possible lacks between naming/code inconsistencies.

All freely configurable within an editor project settings tab, which you can use to change the generated bindings to your will.

## Usage

Include the package as a custom unity package within the manifest or the Unity PackageManager window by using the **#upm** or **#\<tag\>**

    // branch based
    "com.endava.uxmlbindinggenerator": "https://upm-access:eSAr5_wtaBJxbifLy-Wg@git.exozet.com/unitylibs/uitoolkitbindinggenerator.git#upm",

    // tag-based
    "com.endava.uxmlbindinggenerator": "https://upm-access:eSAr5_wtaBJxbifLy-Wg@git.exozet.com/unitylibs/uitoolkitbindinggenerator.git#1.0.0", 

Once added, open the project settings *(Edit/Project Settings/Uxml Building)* and configure the Binding Generator to your project needs. Please create a custom "code binding template" by copying the one within the package path and link it within the script template property (to avoid package update issues)

Further documentation and a Getting started can be found [HERE](./Documentation~/getting-started.md)

## Create package branch (upm)

The upm branch is generated within the CI system, which detects changes in 'master' branch and uses a CI-job to generate the upm branch automatically.
Therefore it is not required to create any package manually. **Just create a merge request and push your changes to the master branch.**

Please update the **[Changelog.md](./Changelog.md)** and **[package.json](./package.json)** whenever necessary. In addition, whenever a new upm branch has been created and you have updated the changelog and package.json version number, create a git-tag of the created upm branch.
