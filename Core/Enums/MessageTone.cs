/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `MessageTone.cs` is an enum file for the kind of line written to the story panel.
    The UI picks a color for each tone, so game code never deals with colors directly.
 */
namespace ARDO.Core.Enums
{
    public enum MessageTone
    {
        Normal,
        Dim,
        Header,
        Command,
        Damage,
        Heal,
        Gold,
        Warning,
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public static class MessageToneExtensions
    {
        public static MessageTone ToTone(this RarityType rarity)
        {
            return rarity switch
            {
                RarityType.Uncommon => MessageTone.Uncommon,
                RarityType.Rare => MessageTone.Rare,
                RarityType.Epic => MessageTone.Epic,
                RarityType.Legendary => MessageTone.Legendary,
                _ => MessageTone.Common
            };
        }
    }
}
