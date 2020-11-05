namespace Dibix.Dapper.Tests
{
    internal sealed class Entity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public Direction? Direction { get; set; }
    }
}