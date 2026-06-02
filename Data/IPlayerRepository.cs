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
    }
}