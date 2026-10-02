using Functional.Domain.Enums.Properties;
using Functional.Api;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: Parallelize(Workers = 1, Scope = ExecutionScope.ClassLevel)]
[assembly: FunctionalMeshProfile(ApplicationProgrammingInterface.Media)]
