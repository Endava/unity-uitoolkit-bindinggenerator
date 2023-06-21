# Version 1.0.0

Ported the proofed Uxml Binding Generator from an existing project to an external package.

**Features**
  - Once imported, specify your Binding settings within the "Edit/Project Settings/Uxml Binding"
    - specify custom Uxml Code Binding template file (cs.txt) (sample/fallback within package)
    - specify custom namespace
    - specify custom Binding class name format
    - specify custom uxml binding type (Proxy, OnClassCreation, LazyLoaded,...)
    - specify custom output directory
    - add ignored folders to your projects
    - verbose logging
    - auto generate on uxml save
  - adds context menu to any *.uxml which allows you to autogenerate code to simplify uxml queries.
  - adds file menu for "Create ALL" or "Sanitize" (Window/Ui Toolkit/ Uxml Binding/...)
  - various samples
  - initial documentation and infrastructure
