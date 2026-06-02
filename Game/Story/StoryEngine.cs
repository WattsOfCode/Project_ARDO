using System;
using ARDO.Core.Abstracts;
using ARDO.Data.SQLite;
using ARDO.Game.Combat;

namespace ARDO.Game.Story
{
    public class StoryEngine
    {
        private readonly QuestRepository _questRepo;
        private readonly CombatEngine _combatEngine;

        public int CurrentQuestId { get; set; }
        public bool HasReachedEnding { get; set; } = false;

        public StoryEngine(QuestRepository questRepo, CombatEngine combatEngine)
        {
            _questRepo = questRepo ?? throw new ArgumentNullException(nameof(questRepo));
            _combatEngine = combatEngine ?? throw new ArgumentNullException(nameof(combatEngine));
        }

        public void RenderCurrentNarrative(Hero player)
        {
            if (player == null) return;

            CurrentQuestId = player.ActiveStoryNode;

            var npcData = _questRepo.GetNpcForQuest(CurrentQuestId);
            var enemyEncounter = _questRepo.GetEnemyForQuest(CurrentQuestId);

            Console.WriteLine("\n==================================================================");
            player.DisplayStatusCard();
            Console.WriteLine("==================================================================");

            if (npcData != null)
            {
                Console.WriteLine($"\n[{npcData.Value.Role}] {npcData.Value.Name}: \"{npcData.Value.Dialogue}\"");
            }

            if (enemyEncounter != null)
            {
                Console.WriteLine($"\n[Encounter] {enemyEncounter.Name} blocks your path.");
                Console.WriteLine("Press any key to enter combat.");
                Console.ReadKey();

                _combatEngine.StartEncounter(player, enemyEncounter);

                if (player.IsDead)
                {
                    Console.WriteLine("\n[Game Over] You perished in battle.");
                    return;
                }
            }

            AdvanceStory(player);
        }

        public void AdvanceStory(Hero player)
        {
            if (CurrentQuestId >= 3)
            {
                HasReachedEnding = true;
                Console.WriteLine("\nCongratulations. You have completed ARDO.");
            }
            else
            {
                player.ActiveStoryNode++;
                Console.WriteLine($"\n[Progression] Area cleared. Story node advanced to {player.ActiveStoryNode}.");
                Console.WriteLine("Press any key to travel onward.");
                Console.ReadKey();
            }
        }
    }
}
