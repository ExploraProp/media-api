using Functional.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: Parallelize(Workers = 1, Scope = ExecutionScope.ClassLevel)]
[assembly: FunctionalMeshProfile(FunctionalMeshProfile.Media)]
