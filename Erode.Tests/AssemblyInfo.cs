using Xunit;

// 禁用测试并行化，确保全局只有一个测试在操作静态变量
// 这样可以避免测试之间的状态污染和竞态条件
[assembly: CollectionBehavior(DisableTestParallelization = true)]

