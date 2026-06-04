/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `IPlayerRepository.cs` is an interface that defines the player persistence 
    contract for create, read, update, and delete operations. It is implemented 
    by the concrete repository classes.
 */
using System;
using System.Data;
using ARDO.Core.Abstracts;

namespace ARDO.Data
{
    public interface IPlayerRepository
    {
        int CreatePlayer(string name, string classType);
        DataTable GetLoadSlots();
        Hero? LoadFullProfile(int characterId);
        void SaveGameState(Hero character, int activeStoryNode);
        void DeleteProfile(int characterId);
        void RemoveInventoryItem(int characterId, int itemId);
    }
}
