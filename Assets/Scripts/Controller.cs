﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    //GameObjects
    public GameObject board;
    public GameObject[] cops = new GameObject[2];
    public GameObject robber;
    public Text rounds;
    public Text finalMessage;
    public Button playAgainButton;

    //Otras variables
    Tile[] tiles = new Tile[Constants.NumTiles];
    private int roundCount = 0;
    private int state;
    private int clickedTile = -1;
    private int clickedCop = 0;

    List<int> robberTiles = new List<int>();
                    
    void Start()
    {        
        InitTiles();
        InitAdjacencyLists();
        state = Constants.Init;
    }

    //Rellenamos el array de casillas y posicionamos las fichas
    void InitTiles()
    {
        for (int fil = 0; fil < Constants.TilesPerRow; fil++)
        {
            GameObject rowchild = board.transform.GetChild(fil).gameObject;            

            for (int col = 0; col < Constants.TilesPerRow; col++)
            {
                GameObject tilechild = rowchild.transform.GetChild(col).gameObject;                
                tiles[fil * Constants.TilesPerRow + col] = tilechild.GetComponent<Tile>();                         
            }
        }
                
        cops[0].GetComponent<CopMove>().currentTile=Constants.InitialCop0;
        cops[1].GetComponent<CopMove>().currentTile=Constants.InitialCop1;
        robber.GetComponent<RobberMove>().currentTile=Constants.InitialRobber;           
    }

    public void InitAdjacencyLists()
    {
        //Matriz de adyacencia
        int[,] matriu = new int[Constants.NumTiles, Constants.NumTiles];

        //Inicializar matriz a 0's
        for(int i = 0; i < Constants.NumTiles; i++)
        {
            for(int j = 0; j < Constants.NumTiles; j++)
            {
                matriu[i, j] = 0;
            }
        }

        //Rellenar la lista "adjacency" de cada casilla con los índices de sus casillas adyacentes
        for(int i = 0; i < Constants.NumTiles; i++)
        {
            for(int j = 0; j < Constants.NumTiles; j++)
            {
                if (i > Constants.NumTiles || i < 0) return;
                else if (i == j + 1 || i == j - 1 || i == j - 8 || i == j + 8)
                {
                    matriu[i, j] = 1;
                    tiles[i].adjacency.Add(j);
                }
                int l = i + 1;
                if(l % 8 == 0 && l != 0 && l < Constants.NumTiles)
                {
                    tiles[i].adjacency.Remove(l);
                    matriu[i, l] = 0;
                }
                int k = i - 1;
                if (i % 8 == 0 && i != 0)
                {
                    tiles[i].adjacency.Remove(i - 1);
                    matriu[i, i - 1] = 0;
                }
                i++;
            }
        }
    }

    //Reseteamos cada casilla: color, padre, distancia y visitada
    public void ResetTiles()
    {        
        foreach (Tile tile in tiles)
        {
            tile.Reset();
        }
    }

    public void ClickOnCop(int cop_id)
    {
        switch (state)
        {
            case Constants.Init:
            case Constants.CopSelected:                
                clickedCop = cop_id;
                clickedTile = cops[cop_id].GetComponent<CopMove>().currentTile;
                tiles[clickedTile].current = true;

                ResetTiles();
                FindSelectableTiles(true);

                state = Constants.CopSelected;                
                break;            
        }
    }

    public void ClickOnTile(int t)
    {                     
        clickedTile = t;

        switch (state)
        {            
            case Constants.CopSelected:
                //Si es una casilla roja, nos movemos
                if (tiles[clickedTile].selectable)
                {                  
                    cops[clickedCop].GetComponent<CopMove>().MoveToTile(tiles[clickedTile]);
                    cops[clickedCop].GetComponent<CopMove>().currentTile=tiles[clickedTile].numTile;
                    tiles[clickedTile].current = true;   
                    
                    state = Constants.TileSelected;
                }                
                break;
            case Constants.TileSelected:
                state = Constants.Init;
                break;
            case Constants.RobberTurn:
                state = Constants.Init;
                break;
        }
    }

    public void FinishTurn()
    {
        switch (state)
        {
            case Constants.TileSelected:
                ResetTiles();

                state = Constants.RobberTurn;
                RobberTurn();
                break;
            case Constants.RobberTurn:                
                ResetTiles();
                IncreaseRoundCount();
                if (roundCount <= Constants.MaxRounds)
                    state = Constants.Init;
                else
                    EndGame(false);
                break;
        }
    }
    
    public void RobberTurn()
    {
        clickedTile = robber.GetComponent<RobberMove>().currentTile;
        tiles[clickedTile].current = true;
        FindSelectableTiles(false);

        //- Elegimos una casilla aleatoria entre las seleccionables que puede ir el caco
        System.Random ran = new System.Random();
        int num = ran.Next(robberTiles.Count);
        int randomList = robberTiles[num];

        //Actualizamos la variable currentTile del caco a la nueva casilla
        robber.GetComponent<RobberMove>().currentTile = randomList;

        //Movemos al caco a esa casilla
        robber.GetComponent<RobberMove>().MoveToTile(tiles[robber.GetComponent<RobberMove>().currentTile]);
    }

    public void EndGame(bool end)
    {
        if(end)
            finalMessage.text = "You Win!";
        else
            finalMessage.text = "You Lose!";
        playAgainButton.interactable = true;
        state = Constants.End;
    }

    public void PlayAgain()
    {
        cops[0].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop0]);
        cops[1].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop1]);
        robber.GetComponent<RobberMove>().Restart(tiles[Constants.InitialRobber]);

        ResetTiles();

        playAgainButton.interactable = false;
        finalMessage.text = "";
        roundCount = 0;
        rounds.text = "Rounds: ";

        state = Constants.Restarting;
    }

    public void InitGame()
    {
        state = Constants.Init;

    }

    public void IncreaseRoundCount()
    {
        roundCount++;
        rounds.text = "Rounds: " + roundCount;
    }

    public void FindSelectableTiles(bool cop)
    {  
        int indexcurrentTile;        

        if (cop==true)
            indexcurrentTile = cops[clickedCop].GetComponent<CopMove>().currentTile;
        else
            indexcurrentTile = robber.GetComponent<RobberMove>().currentTile;

        robberTiles.Clear();

        //La ponemos rosa porque acabamos de hacer un reset
        tiles[indexcurrentTile].current = true;

        //Cola para el BFS
        Queue<Tile> nodes = new Queue<Tile>();

        tiles[indexcurrentTile].visited = true;
        tiles[indexcurrentTile].distance = 0;
        tiles[indexcurrentTile].parent = null;

        nodes.Enqueue(tiles[indexcurrentTile]);

        Tile before;

        while(nodes.Count != 0)
        {
            before = nodes.Dequeue();
            int before2 = before.numTile;

            foreach (int adyacent in tiles[before2].adjacency)
            {
                if (tiles[adyacent].visited == false)
                {
                    tiles[adyacent].visited = true;
                    tiles[adyacent].distance = tiles[before2].distance + 1;
                    tiles[adyacent].parent = tiles[before2];
                    nodes.Enqueue(tiles[adyacent]);

                    if (tiles[adyacent].distance <= 2)
                    {
                        if (cop == false)
                        {
                            robberTiles.Add(tiles[adyacent].numTile);
                        }
                        tiles[adyacent].selectable = true;
                        if (cops[0].GetComponent<CopMove>().currentTile == tiles[adyacent].numTile || cops[1].GetComponent<CopMove>().currentTile == tiles[adyacent].numTile)
                        {
                            tiles[adyacent].selectable = false;
                        }
                    }
                }
            }
        }
    }  
}
