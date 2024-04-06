using YAVCS;

//TODO: Add logging in each command
//TODO: PackFiles
//TODO: Refactor Code on exceptions and remove useless code
//TODO: Comments
//TODO: Parallel executing and smart work with index without rewriting file every time
//TODO: Think about statuses(staged/unmodified/modified/untracked) in indexRecord and edit index after commit

App.Configure();
App.Run(args);

