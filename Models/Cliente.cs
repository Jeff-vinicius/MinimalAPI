namespace MinimalAPI.Models
{
    public class Cliente
    {
        public Guid Id { get; set; }
        public string? Nome { get; set; }
        public string? Documento { get; set; }
        public string? Telefone { get; set; }
        public bool Ativo { get; set; }
    }
}
