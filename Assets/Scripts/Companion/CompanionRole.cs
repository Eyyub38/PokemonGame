public enum CompanionBondLevel {
    Stranger,
    Familiar,
    Friend,
    Trusted,
    Soulbound
}

public static class CompanionBondRules {
    public static CompanionBondLevel GetBondLevel(int bondPoints) {
        if(bondPoints >= 800) return CompanionBondLevel.Soulbound;
        if(bondPoints >= 400) return CompanionBondLevel.Trusted;
        if(bondPoints >= 180) return CompanionBondLevel.Friend;
        if(bondPoints >= 50) return CompanionBondLevel.Familiar;
        return CompanionBondLevel.Stranger;
    }

    public static int GetBondMultiplier(CompanionBondLevel bondLevel) {
        switch(bondLevel) {
            case CompanionBondLevel.Familiar: return 2;
            case CompanionBondLevel.Friend: return 3;
            case CompanionBondLevel.Trusted: return 4;
            case CompanionBondLevel.Soulbound: return 5;
            default: return 1;
        }
    }
}
