using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CardSuit
{
    SPADE,
    CLOVER,
    DIAMOND,
    HEART,
    BLACK,
    RED
}

public enum CardValue
{
    ACE,
    TWO,
    THREE,
    FOUR,
    FIVE,
    SIX,
    SEVEN,
    EIGHT,
    NINE,
    TEN,
    JACK,
    QUEEN,
    KING,
    JOKER
}



public class CardComparer : IEqualityComparer<Card>
{
   

    public bool Equals(Card x, Card y)
    {
        return x.Equals(y);
    }
    
    public int GetHashCode(Card obj)
    {
        string cardHash = Card.suits[obj.cardSuit] + " " + Card.values[obj.cardValue];
        return cardHash.GetHashCode();
    }
}

public class Card
{
    public static Dictionary<Card, string> CARDS = new Dictionary<Card, string>(new CardComparer());
    static bool initialized = false;
    public static string[] suits = { "CLOVER", "DIAMOND", "HEART", "SPADE", "BLACK", "RED" };
    public static string[] values = { "JOKER", "TWO", "THREE", "FOUR", "FIVE", "SIX", "SEVEN", "EIGHT", "NINE", "TEN", "JACK", "QUEEN", "KING", "ACE" };
    //public Sprite cardSprite;

    public static void Init()
    {
        for (int i = 0; i < 4; i++)
        {
            for (int j = 1; j <= 13; j++)
            {
                Card c = new Card();
                c.cardSuit = i;
                c.cardValue = j;
                CARDS.Add(c, suits[i] + " " + values[j]);
            }
        }
        for(int i = 4; i < 6; i++)
        {
            Card c = new Card();
            c.cardSuit = i;
            c.cardValue = 0;
            CARDS.Add(c, suits[i] + " " + values[0]);
        }
    }

    public Card()
    {
        if (!initialized)
        {
            Debug.Log("CARDS INITIALIZED");
            initialized = true;
            Init();
        }
    }

    public int cardSuit, cardValue;

    public bool Equals(Card c)
    {
        return c != null && cardSuit == c.cardSuit && cardValue == c.cardValue;
    }

    public int Difference(Card c)
    {
        return cardValue - c.cardValue;
    }

    public bool EqualSuit(Card c)
    {
        return c != null && cardSuit == c.cardSuit;
    }

    public bool EqualValue(Card c)
    {
        return c != null && cardValue == c.cardValue;
    }

    public static Card NameToCard(string name)
    {
        string[] splitName = name.Split(' ');
        Card c = new Card();
        for (int i = 0; i < suits.Length; i++)
        {
            if (splitName[0].Equals(suits[i]))
            {
                c.cardSuit = i;
                break;
            }
        }
        for (int i = 0; i < values.Length; i++)
        {
            if (splitName[1].Equals(values[i]))
            {
                c.cardValue = i;
                break;
            }
        }
        return c;
    }

    public static Card ToCard(string s)
    {
        string[] splitData = s.Split(',');
        return ToCard(int.Parse(splitData[0]),  int.Parse(splitData[1]));
    }

    public static Card ToCard(int suit, int value)
    {
        if (suit < 0 || suit > suits.Length || value < 0 || value > values.Length)
            return null;
        Card card = new Card();
        card.cardSuit = suit;
        card.cardValue = value;
        return card;
    }

    public override string ToString()
    {
        return cardSuit + "," + cardValue;
    }
}

// Card Suits 0-3 = Spade, Clover, Diamond, Heart. 4-5 = Black, Red
// Card Values 0-13 = Joker, Ace, Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King
public class Deck {
    private List<Card> deck;
    private bool useJokers = false;
    private bool isShuffled = false;
    private bool isTouched = false;

    public Deck()
    {
        deck = new List<Card>();
        /*
        for (int i = 0; i < 4; i++)
        {
            for (int j = 1; j <= 3; j++)
            {
                Card c = new Card();
                c.cardSuit = i;
                c.cardValue = j;
                //Debug.Log(CardSuit.SPADE);
                //Debug.Log(">" + CardSuit.RED);
                deck.Add(c);
            }
        }
        */
        ///*
        for(int i = 0; i < 4; i++)
        {
            for(int j = 1; j <= 13; j++)
            {
                Card c = new Card();
                c.cardSuit = i;
                c.cardValue = j;
                //Debug.Log(CardSuit.SPADE);
                //Debug.Log(">" + CardSuit.RED);
                deck.Add(c);
            }
        }
        //*/
        //foreach (Card c in deck)
        //    Debug.Log(c.cardSuit + " " + c.cardValue);
    }

    public Deck(List<string> cards)
    {
        deck = new List<Card>();
        foreach(string card in cards)
        {
            Card c = Card.ToCard(card);
            deck.Add(c);
        }
    }

    public override string ToString()
    {
        string s = "";
        foreach(Card c in deck)
        {
            s += c.ToString() + "|";
        }
        s = s.Trim('|');
        return s;
    }

    public void Shuffle()
    {
        List<Card> shuffled = new List<Card>();
        while(deck.Count > 0)
        {
            int card = UnityEngine.Random.Range(0, deck.Count);
            shuffled.Add(deck[card]);
            deck.Remove(deck[card]);
        }
        deck = shuffled;
        isShuffled = true;
        //foreach (Card c in deck)
        //    Debug.Log(c.cardSuit + " " + c.cardValue);
    }

    public void UseJokers()
    {
        if (useJokers) {
            Debug.LogError("Already using joker cards");
            return;
        }
        Card c = new Card();
        c.cardSuit = 4;
        c.cardValue = 0;
        deck.Add(c);
        c = new Card();
        c.cardSuit = 5;
        c.cardValue = 0;
        deck.Add(c);
        useJokers = true;
    }

    public List<Card> Draw(int count)
    {
        List<Card> cards = new List<Card>();
        while (count > 0 && deck.Count > 0)
        {
            Card c = deck[0];
            cards.Add(c);
            deck.Remove(c);
            count--;
            isTouched = true;
        }
        return cards;
    }

    public void AddToTop(Card c)
    {
        deck.Insert(0, c);
    }

    public void AddToBottom(Card c)
    {
        deck.Add(c);
    }

    public int Count()
    {
        return deck.Count;
    }

    public bool IsEmpty()
    {
        return deck.Count == 0;
    }

    public bool UsingJoker()
    {
        return useJokers;
    }

    public bool IsShuffled()
    {
        return isShuffled;
    }
    
    /*
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}*/
    }
