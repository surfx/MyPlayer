using System.Text.Json;

namespace MyPlayer.classes.playlist
{
    internal class PlayList
    {
        public required string Nome { get; set; }
        public List<MusicaDTO> Musicas { get; set; } = new();

        // Método para salvar a playlist atual em um arquivo JSON
        public static void Salvar(string caminhoArquivo, List<MusicaDTO> musicas)
        {
            JsonSerializerOptions opcoes = new() { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(musicas, opcoes);
            File.WriteAllText(caminhoArquivo, jsonString);
        }

        // Método para carregar uma playlist de um arquivo JSON
        public static List<MusicaDTO> Carregar(string caminhoArquivo)
        {
            if (!File.Exists(caminhoArquivo)) return [];

            string jsonString = File.ReadAllText(caminhoArquivo);
            return JsonSerializer.Deserialize<List<MusicaDTO>>(jsonString) ?? new();
        }
    }
}