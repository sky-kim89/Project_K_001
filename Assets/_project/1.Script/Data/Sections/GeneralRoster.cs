// ============================================================
//  GeneralRoster.cs
//  배치 장수 명부를 바꾸는 조작의 정본.
//
//  ■ 왜 별도 파일인가
//    해고는 세이브 섹션 세 개(DeploymentData·UnitData)와 시너지 재계산,
//    저장 요청까지 한 묶음이다. 게다가 "마지막 1명은 해고 불가" 라는 보호 규칙이
//    붙어 있다 — 전부 해고하면 전투를 시작할 수 없다.
//
//    이걸 화면마다 다시 적으면 보호 규칙이 두 벌이 된다. 실제로 그럴 뻔했다:
//    HeroDetailPopup 이 갖고 있던 것을 용병 고용 팝업이 그대로 복사할 참이었다.
//    한쪽만 고치면 어떤 경로로는 마지막 장수까지 해고돼 판이 잠긴다.
//
//  ■ 쓰는 곳
//    HeroDetailPopup   — [해고] 버튼
//    MercenaryShopPopup — 현재 부대 칸 아래 [해고] 버튼
// ============================================================

public static class GeneralRoster
{
    /// <summary>
    /// 해고할 수 있는가 — 배치된 장수가 2명 이상이어야 한다.
    ///
    /// ⚠ '슬롯이 몇 칸 열렸나' 가 아니라 '몇 명이 서 있나' 다
    ///   슬롯을 기준으로 재면 5칸 중 1명만 배치된 상태에서도 해고가 열려
    ///   부대가 비어 버린다.
    /// </summary>
    public static bool CanFire()
    {
        var deploy = UserDataManager.Instance?.Get<DeploymentData>();
        return deploy != null && deploy.GetDeployedUnits().Count > 1;
    }

    /// <summary>
    /// 배치 장수를 해고한다. 마지막 1명이면 아무 일도 하지 않고 false.
    /// 명부에서 빼고, 시너지를 다시 계산하고, 저장까지 요청한다.
    /// </summary>
    public static bool Fire(string unitName)
    {
        if (string.IsNullOrEmpty(unitName)) return false;
        if (!CanFire())                     return false;

        var deploy = UserDataManager.Instance.Get<DeploymentData>();
        var units  = UserDataManager.Instance.Get<UnitData>();

        deploy.Undeploy(unitName);
        units.RemoveUnit(unitName);

        // 부대 구성이 바뀌면 직업 시너지 특성이 붙었다 떨어진다 — 빼먹으면
        // 해고한 장수 몫의 시너지가 전투까지 따라간다.
        JobSynergyEvaluator.Recalculate();
        UserDataManager.Instance.RequestSave();
        return true;
    }
}
