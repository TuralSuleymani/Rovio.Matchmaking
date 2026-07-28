Add integration tests for @<TargetSurface> following @docs/coding-standards/coding-standards/integration-testing-standards.md and sibling tests in the same suite.

Correct project (Infra ports vs API HTTP). 
RequireDocker first; 
real Testcontainers fixture; 
no mocks; Unique* IDs;
factories/MatchmakingTestData; Result or HttpStatusCode assertions; RunOnceAsync for matching. 
Add only missing scenarios, then run the suite filter and fix.