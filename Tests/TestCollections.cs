namespace Tests;

public static class TestCollections {

    [CollectionDefinition(Name, DisableParallelization = true)]
    public class CwdSensitive {

        public const string Name = "CWD Sensitive";

    }

}