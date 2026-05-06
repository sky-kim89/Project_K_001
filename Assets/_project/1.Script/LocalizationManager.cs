using System.Collections.Generic;

// ============================================================
//  LocalizationManager.cs
//  enum.ToString() 을 key 로 현지화 텍스트를 반환.
//
//  사용:
//    LocalizationManager.Instance.Get(stat.ToString())
//    LocalizationManager.Instance.Get(skillId.ToString())
//    LocalizationManager.Instance.Get(grade.ToString())
//
//  다국어 대응:
//    BuildTable() 을 언어별로 교체하거나
//    JSON 파일에서 Dictionary 로드 하도록 확장.
// ============================================================

public class LocalizationManager : SingletonPure<LocalizationManager>
{
    readonly Dictionary<string, string> _table;

    public LocalizationManager()
    {
        _table = BuildKorean();
    }

    public string Get(string key)
        => _table.TryGetValue(key, out var v) ? v : key;

    static Dictionary<string, string> BuildKorean() => new()
    {
        // ── StatType ──────────────────────────────────────────
        { "MaxHp",        "체력"   },
        { "Attack",       "공격"   },
        { "Defense",      "방어율" },
        { "MoveSpeed",    "이속"   },
        { "AttackSpeed",  "공속"   },
        { "AttackRange",  "사거리" },
        { "SoldierCount", "용병수" },
        { "CommandPower", "지휘력" },

        // ── EquipmentTrigger ──────────────────────────────────
        { "OnAttack", "공격 시" },
        { "OnHit",    "피격 시" },

        // ── ActiveSkillId ─────────────────────────────────────
        { "HeavyStrike",      "강타"          },
        { "VolleyFire",       "일제 사격"     },
        { "LeapStrike",       "도약 강타"     },
        { "HealAura",         "치유 오라"     },
        { "TargetHeal",       "집중 치유"     },
        { "ChargeSoldier",    "돌격 병사"     },
        { "SummonSkeleton",   "스켈레톤 소환" },
        { "PoisonZone",       "독성 지대"     },
        { "Meteor",           "메테오"        },
        { "Blizzard",         "블리자드"      },
        { "SacrificeSoldier", "병사 희생"     },
        { "Bind",             "속박"          },
        { "SuicideSoldier",   "자폭 병사"     },
        { "Berserker",        "광전사"        },
        { "IronShield",       "철벽 방어"     },
        { "ArrowRain",        "화살 비"       },
        { "BattleCry",        "전투 함성"     },
        { "Shockwave",        "충격파"        },
        { "SwiftStrike",      "신속 연격"     },
        { "SummonElite",      "정예 소환"     },

        // ── UnitGrade ─────────────────────────────────────────
        { "Normal",   "일반" },
        { "Uncommon", "비범" },
        { "Rare",     "희귀" },
        { "Unique",   "고유" },
        { "Epic",     "영웅" },

        // ── UnitJob ───────────────────────────────────────────
        { "Knight",       "기사"   },
        { "Archer",       "궁수"   },
        { "Mage",         "마법사" },
        { "ShieldBearer", "방패병" },
    };
}
