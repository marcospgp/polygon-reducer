# nuget-manually-downloaded-packages

These packages contain code analyzers for C#. They are referenced in `omnisharp.json` in order to work correctly with VSCode.

List of packages in use:

* <https://www.nuget.org/packages/Microsoft.CodeAnalysis.NetAnalyzers>
* <https://www.nuget.org/packages/Microsoft.Unity.Analyzers>
* <https://www.nuget.org/packages/Roslynator.Analyzers>
* <https://www.nuget.org/packages/Roslynator.Formatting.Analyzers>

Unity [does not support installing nuget packages automatically](https://docs.microsoft.com/en-us/visualstudio/gamedev/unity/unity-scripting-upgrade?view=vs-2017#add-packages-from-nuget-to-a-unity-project) at the time of writing. Packages have to be updated periodically by manually downloading them from the nuget website.

Please note that when updating a package `omnisharp.json` has to be updated with the new folder name. The folder name will change as it contains the version number.
