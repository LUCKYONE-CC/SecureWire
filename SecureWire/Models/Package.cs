namespace SecureWire.Models
{
    public class Package<T>
    {
        public Flags FLAG { get; set; }
        public T Value { get; set; }
    }
}
