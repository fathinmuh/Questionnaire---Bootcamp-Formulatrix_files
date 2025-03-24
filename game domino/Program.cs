﻿﻿﻿using System;
// using dominoesGame;

public interface IDeck{
    void Shuffle();
    Card DrawCard();
}
public class Deck:IDeck{
    public List<Card> Cards { get; private set; }
    public Deck()
    {
        Cards = new List<Card>();
        int id = 1;

        for (int i = 0; i <= 6; i++)
        {
            for (int j = i; j <= 6; j++)
            {
                Cards.Add(new Card(id++, i, j));
            }
        }
    }

    public void Shuffle(){
        Random rng = new Random();
        int n = Cards.Count;
        for (int i = n - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (Cards[i], Cards[j]) = (Cards[j], Cards[i]); // Swap kartu
        }
    }
    public Card DrawCard()
    {
        if (Cards.Count == 0) throw new InvalidOperationException("Deck kosong!");
        
        Card drawnCard = Cards[0];
        Cards.RemoveAt(0);
        return drawnCard;
    }
}

public interface ICard{
    int Id{get;}
    int FirstFaceValue{get;set;}
    int SecondFaceValue{get;set;}
}
public class Card : ICard
{
    public int Id { get; }
    public int FirstFaceValue { get; set;}
    public int SecondFaceValue { get; set;}
    public Card(int id, int first, int second)
    {
        Id = id;
        FirstFaceValue = first;
        SecondFaceValue = second;
    }

    public bool IsDouble()
    {
        return FirstFaceValue == SecondFaceValue;
    }

    public int GetHighestValue()
    {
        return Math.Max(FirstFaceValue, SecondFaceValue);
    }

    public override string ToString()
    {
        return $"[{FirstFaceValue}|{SecondFaceValue}]";
    }
}
public interface IPlayer{
    int Id{get;}
    string Name{get;}
    // bool IsBot{get;}
}
public class Player:IPlayer{
    public int Id{get;}
    public string Name{get;}
    public Player(int id, string name){
        Id = id;
        Name = name;
    }
    public override string ToString()
    {
        return Name;
    }
}

public interface IBoard{
    void UpdateBoard(Card card, IPlayer player, bool posisi);
    List<Card> GetBoard();
}

public class Board:IBoard{
    private List<Card> playedCards;
    public void UpdateBoard(Card card, IPlayer player, bool posisi){
        if(posisi){
            playedCards.Add(card);
        }
        else {
            playedCards.Insert(0, card);
        }
    }
    public List<Card> GetBoard(){
        return playedCards;
    }
    public Board()
    {
        playedCards = new List<Card>();
    }
}

public class GameController{ 
    private IDisplay display;   
    private IDeck deck;    
    private IBoard board;    
    private List<IPlayer> players;    
    private Dictionary<IPlayer,List<Card>> hand;    
    private Dictionary<int, Card>? moveOptions;    
    private IPlayer? currentPlayer;    
    private int currentPlayerIndex=0;    
    Action? onGameStart;    
    Action<IPlayer>? onPlayerTurn;    
    public Action<bool>? onGameOver;

    public GameController(IDeck deck)    
    {        
        this.deck = deck;        
        this.deck.Shuffle();        
        players = new List<IPlayer>();        
        hand = new Dictionary<IPlayer, List<Card>>();        
        board=new Board(); 
        display=new Display();   
    }    
    public void StartGame(Action onGameStart)    
    {        
        onGameStart?.Invoke();    
    }

    public void AssignPlayers(List<IPlayer> playerList)    
    {        
        players.AddRange(playerList);        
        foreach (var player in players)        
        {            
            hand[player] = new List<Card>();        
        }    
    }
    public void DealCards(int cardsPerPlayer)    
    {        
        foreach (var player in players)        
        {            
            for (int i = 0; i < cardsPerPlayer; i++)            
            {                
                if (deck == null) break;                
                hand[player].Add(deck.DrawCard());            
            }        
        }    
    }
    public void PlayCard(IPlayer player, Card card, bool placeRight)    
    {        
        List<Card> boardCards = board.GetBoard();
        if (boardCards.Count > 0)        
        {            
            int leftValue = boardCards.First().FirstFaceValue;            
            int rightValue = boardCards.Last().SecondFaceValue;
            if (!placeRight && card.SecondFaceValue != leftValue)        
            {            
                (card.FirstFaceValue, card.SecondFaceValue) = (card.SecondFaceValue, card.FirstFaceValue);        
            }
            if (placeRight && card.FirstFaceValue != rightValue)        
            {            
                (card.FirstFaceValue, card.SecondFaceValue) = (card.SecondFaceValue, card.FirstFaceValue);        
            }
        }
        
        hand[player].Remove(card);        
        board.UpdateBoard(card, player, placeRight);            
    }

    public List<Card> GetBoardState()
    {
        return board.GetBoard();
    }
    public Dictionary<IPlayer, List<Card>> GetHandValue()    
    {        
        return hand;    
    }
    public IPlayer DetermineFirstPlayer()    
    {        
        var highestDouble = players            
            .SelectMany(p => hand[p], (player, card) => new { player, card })            
            .Where(x => x.card.IsDouble())            
            .OrderByDescending(x => x.card.GetHighestValue())            
            .FirstOrDefault();
        if (highestDouble != null)
            return highestDouble.player; 

        var highestValueCard = players
            .SelectMany(p => hand[p], (player, card) => new { player, card })
            .OrderByDescending(x => Math.Max(x.card.FirstFaceValue, x.card.SecondFaceValue)) // Cari kartu dengan nilai tertinggi
            .FirstOrDefault();

        return highestValueCard?.player;  
    }
    public void RandomizeTurnOrder(IPlayer currentPlayer)    
    {        
        if (players.Count > 1)        
        {            
            players = players
            .Where(p => p != currentPlayer)
            .OrderBy(_ => Guid.NewGuid()).ToList();            
            players.Insert(0, currentPlayer);        
        }
    }    
    public void NextTurn(Action<IPlayer> onPlayerTurn)    
    {        
        if (players.Count == 0) return;
        int startIndex = currentPlayerIndex;
        do        
        {            
            currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;            
            currentPlayer = players[currentPlayerIndex];
            var moveOptions = GetPlayableMoves(currentPlayer);            
            if (moveOptions.Any()) break; // Jika pemain bisa bermain, lanjutkan            
            Console.WriteLine($"{currentPlayer.Name} tidak bisa bermain, giliran dilewati.");        
        }         
        // while (true); // Loop sampai ada pemain yang bisa bermain
        while (currentPlayerIndex != startIndex);
        onPlayerTurn(currentPlayer);    
    }
    public Dictionary<int, (Card,bool canPlaceLeft, bool canPlaceRight)> GetPlayableMoves(IPlayer player)    
    {        
        List<Card> boardCards = board.GetBoard();        
        var moveOptions = new Dictionary<int, (Card,bool,bool)>();        
        if (boardCards.Count == 0)        
        {           
            foreach (var card in hand[player])            
            {                
                moveOptions[card.Id] = (card, true, true);            
            }            
            return moveOptions;        
        }                
        int leftValue = boardCards.First().FirstFaceValue;    //ambil nilai pojok kiri    
        int rightValue = boardCards.Last().SecondFaceValue;   //ambil nilai pojok kanan
        foreach (Card card in hand[player])        
        {            
            bool canPlaceLeft = (card.FirstFaceValue == leftValue || card.SecondFaceValue == leftValue);            
            bool canPlaceRight = (card.FirstFaceValue == rightValue || card.SecondFaceValue == rightValue);
            if (canPlaceLeft || canPlaceRight)            
            {               
                moveOptions[card.Id] = (card, canPlaceLeft, canPlaceRight);            
            }        
        }        
        return moveOptions;            
    }

    public bool CheckGameOver()
    {
        return players.Any(p => hand[p].Count == 0) || players.All(p => GetPlayableMoves(p).Count == 0);
    }

    public int GetValue(Card card)
    {
        return card.FirstFaceValue + card.SecondFaceValue;
    }

    public int GetScore(IPlayer player)
    {
        return hand[player].Sum(card => GetValue(card));
    }

    public IPlayer GetWinner()
    {
        foreach (var player in players)
        {
            if (hand[player].Count == 0)
            {
                return player;
            }
        }

        var playerScores = players
            .Select(p => new
            {
                Player = p,
                TotalPoints = GetScore(p)
            })
            .OrderBy(p => p.TotalPoints) // Urutkan berdasarkan total nilai terkecil
            .ToList();

        return playerScores.First().Player;
    }

    public void GameOver()
    {
        bool isGameOver = CheckGameOver();

        onGameOver?.Invoke(isGameOver);
    }

}

public interface IDisplay{

    int boardCursorTop{get;set;}
    void ShowBoard(List<Card> board);
    int SetupPlayers();
    string AssignPlayersName(int playerNumber);
    void ShowCurrentPlayer(IPlayer currentPlayer);
    Card ShowHand(IPlayer player, List<Card> playerHand, Dictionary<int, (Card card, bool canPlaceLeft, bool canPlaceRight)> moveOptions);
    bool AssignPlacementSide(bool canPlaceLeft, bool canPlaceRight);
    // void ShowWinner(IPlayer player);
    void ShowMessage(string message);
}
public class Display:IDisplay{    
    public int boardCursorTop {get;set;}    
    public void ShowBoard(List<Card> board)    
    {        
        for (int i = 0; i < Console.WindowHeight; i++)        
        {            
            Console.SetCursorPosition(0, boardCursorTop + i);            
            Console.Write(new string(' ', Console.WindowWidth));                    
        }        
        Console.Clear();        
        Console.WriteLine("=== Board Saat Ini ===");
        if (board.Count == 0)        
        {                
            Console.WriteLine("[ Board Kosong ]");            
            return;         
        }
        List<string[]> boardRows = new List<string[]>();
        foreach (var card in board)        
        {            
            string[] cardDisplay;            
            int first = card.FirstFaceValue;            
            int second = card.SecondFaceValue;
            if (first == second) // Kartu double (vertikal)            
            {                
                cardDisplay = new string[]                
                {                    
                    "┌───┐",                    
                    $"│ {first} │",                    
                    $"│ {second} │",                    
                    "└───┘"                
                };            
            }            
            else // Kartu biasa (horizontal)            
            {                
                cardDisplay = new string[]                
                {                    
                    "┌───┬───┐",                    
                    $"│ {first} │ {second} │",                    
                    "└───┴───┘"                
                };            
            }
            boardRows.Add(cardDisplay);        }
            // Gabungkan kartu dalam satu tampilan
        
            // Hitung total panjang kartu jika ditampilkan semua        
            int totalWidth = boardRows.Sum(c => c[0].Length + 1); // +1 untuk spasi antar kartu        
            int consoleWidth = Console.WindowWidth;

            // Jika terlalu panjang, sembunyikan kartu tengah        
            bool needShorten = totalWidth > consoleWidth;        
            int maxRows = boardRows.Max(c => c.Length);
            for (int row = 0; row < maxRows; row++)        
            {            
                for (int i = 0; i < boardRows.Count; i++)            
                {                
                    // Tampilkan hanya kartu pertama, terakhir, dan "..." di tengah                
                    if (needShorten && i > 0 && i < boardRows.Count - 1)                
                    {                    
                        if (i == 1) // Cetak "..." hanya sekali                    
                        {                        
                            Console.Write("  ...   ");                    
                        }                    
                        continue;                
                    }
                    string[] card = boardRows[i];
                // Cetak bagian kartu                
                    if (row < card.Length)                    
                        Console.Write(card[row] + " ");                
                    else                    
                        Console.Write(new string(' ', card[0].Length) + " ");            
                }            
                Console.WriteLine();        
            }    
    }    
    public int SetupPlayers()    
    {       
        Console.Write("Masukkan jumlah pemain (2-4): ");        
        int numPlayers;        
        int maxPlayer = 4;        
        while (!int.TryParse(Console.ReadLine(), out numPlayers) || numPlayers < 2 || numPlayers > maxPlayer)        
        {            
            Console.Write("Input tidak valid! Masukkan jumlah pemain antara 2-4: ");        
        }        
        return numPlayers;    
    }
    public string AssignPlayersName(int playerNumber)    
    {        
        Console.Write($"Masukkan nama untuk Pemain {playerNumber}: ");        
        return Console.ReadLine();    
    }
    public void ShowCurrentPlayer(IPlayer currentPlayer)    
    {        
        Console.WriteLine(currentPlayer != null ? $"{currentPlayer.Name} mulai duluan!" : "Tidak ada yang punya kartu double, pilih pemain pertama secara acak.");    
    }    
    public Card ShowHand(IPlayer player, List<Card> playerHand, Dictionary<int, (Card card, bool canPlaceLeft, bool canPlaceRight)> moveOptions)    
    {        
        Console.WriteLine();        
        Console.WriteLine($"{player.Name}, pilih kartu yang akan dimainkan:");                
        for (int i = 0; i < playerHand.Count; i++)        
        {            
            Card card = playerHand[i];            
            bool isPlayable = moveOptions.ContainsKey(card.Id);
            if (isPlayable)                
                Console.ForegroundColor = ConsoleColor.Green; // Kartu bisa dimainkan            
            else                
                Console.ForegroundColor = ConsoleColor.White; // Tidak bisa
            Console.Write($"{i + 1}. {card}  ");        
        }                
        Console.ResetColor();        
        Console.WriteLine();
        int choice;        
        while (true)        
        {            
            Console.Write("Pilih nomor kartu: ");            
            if (int.TryParse(Console.ReadLine(), out choice) && choice >= 1 && choice <= playerHand.Count)            
            {                
                Card chosenCard = playerHand[choice - 1];
                if (moveOptions.ContainsKey(chosenCard.Id)) // Hanya izinkan kartu yang bisa dimainkan                    
                    return chosenCard;
                Console.WriteLine("Kartu ini tidak bisa dimainkan! Pilih kartu yang valid.");            
            }            
            else            
            {                
                Console.WriteLine("Pilihan tidak valid, coba lagi.");            
            }        
        }    
    }     
    public bool AssignPlacementSide(bool canPlaceLeft, bool canPlaceRight){        
        while (true)        
        {            
            Console.WriteLine("Pilih sisi untuk meletakkan kartu (kiri/kanan):");
            string? input = Console.ReadLine()?.ToLower();
            if (input == "kiri" && canPlaceLeft) return false;            
            if (input == "kanan" && canPlaceRight) return true;
            Console.WriteLine("Pilihan tidak valid! Pilih sisi yang sesuai dengan kartu.");        
        }    
    }
    public void ShowMessage(string message){
        Console.WriteLine(message);
    }

}

public class Program{    
    public static void Main(string[] args)    
    {        
        Deck deck = new Deck();        
        GameController gameController = new GameController(deck);
        IDisplay display = new Display();

        gameController.StartGame(() =>        
        {            
            int numPlayers = display.SetupPlayers();            
            var players = new List<IPlayer>();
            for (int i = 1; i <= numPlayers; i++)            
            {                
                string playerName = display.AssignPlayersName(i);                
                players.Add(new Player(i, playerName));            
            }
            gameController.AssignPlayers(players);            
            gameController.DealCards(5);
            var currentPlayer = gameController.DetermineFirstPlayer();            
            // gameController.SetCurrentPlayer(currentPlayer);
            display.ShowCurrentPlayer(currentPlayer);            
            gameController.RandomizeTurnOrder(currentPlayer);
            if (currentPlayer != null)            
            {                
                var playerHand = gameController.GetHandValue()[currentPlayer];                
                if (playerHand.Any())                
                {                    
                    var moveOptions = gameController.GetPlayableMoves(currentPlayer);                    
                    Card chosenCard = display.ShowHand(currentPlayer, playerHand, moveOptions);                    
                    bool posisi = true;                    
                    gameController.PlayCard(currentPlayer, chosenCard, posisi);
                    display.ShowBoard(gameController.GetBoardState());                
                }            
            }
        });
        bool gameRunning = true;         
        while (gameRunning)        
        {            
            gameController.NextTurn(player =>            
            {                
                var moveOptions = gameController.GetPlayableMoves(player);                
                var playerHand = gameController.GetHandValue()[player];
                if (moveOptions.Any())                
                {                    
                    Card chosenCard = display.ShowHand(player, playerHand, moveOptions);
                    var (_, canPlaceLeft, canPlaceRight) = moveOptions[chosenCard.Id];
                    bool posisi = display.AssignPlacementSide(canPlaceLeft, canPlaceRight);                    
                    gameController.PlayCard(player, chosenCard, posisi);
                    display.ShowBoard(gameController.GetBoardState());                
                }                
                else                
                {
                    display.ShowMessage("Tidak ada yang bisa mengeluarkan kartu");                          
                    // gameController.CheckGameOver();
                }            
                
                gameController.onGameOver = (isGameOver) =>
                {
                    if (isGameOver)
                    {
                        IPlayer winner = gameController.GetWinner();
                        display.ShowMessage($"Pemenangnya adalah: {winner.Name}");
                    }
                };

                // Panggil GameOver tanpa parameter
                if (gameController.CheckGameOver())
                {
                    gameController.GameOver();
                }
           
            });
        }
    }
}