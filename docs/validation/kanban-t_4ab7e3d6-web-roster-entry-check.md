# Kanban t_4ab7e3d6：Web 可见入口与员工元数据展示检查

验证时间：2026-05-22 14:40:36 +0800
执行员工：冯前端 / Web 前端工程师 / frontend-eng

## 结论

PASS：Hermes Web Dashboard 与 CLI 的员工展示路径仍符合 PrivateCloudDrive 多员工协作约定。

- 员工中文姓名作为主识别信息：`display_name`
- 英文 profile ID 作为辅助识别信息：`name`
- 岗位展示：`role`
- 职责展示：`responsibilities`
- 26 个 Hermes profile 的 `profile.yaml` 元数据完整，无缺失项

## 可见入口检查

| 入口 | 检查点 | 结果 |
|---|---|---|
| Hermes Web Dashboard `/profiles` | `ProfilesPage.tsx` 使用 `display_name` 作为主标签，profile ID 作为辅助信息 | PASS |
| Hermes Web Dashboard `/profiles` | 页面展示 `岗位：{role}` | PASS |
| Hermes Web Dashboard `/profiles` | 页面展示 `responsibilities` 职责 chips | PASS |
| Web API `/api/profiles` | 返回 `display_name`、`role`、`responsibilities`、`description` | PASS |
| CLI `hermes profile list` | 输出 `员工 / 岗位` 列，含中文姓名和岗位 | PASS |
| Hermes profile.yaml | 26 个员工 profile 元数据完整 | PASS |

## 验证命令与证据

### 1. profile.yaml 元数据完整性

命令：

```bash
python - <<'PY'
import pathlib, yaml, json
root=pathlib.Path(r'C:/Users/q4528/AppData/Local/hermes')
profiles=[]
for name, p in [('default', root)] + [(p.name,p) for p in sorted((root/'profiles').iterdir()) if p.is_dir()]:
    data=yaml.safe_load((p/'profile.yaml').read_text(encoding='utf-8')) or {}
    profiles.append({
        'name':name,
        'display_name':data.get('display_name'),
        'role':data.get('role'),
        'responsibilities':data.get('responsibilities'),
        'description_auto':data.get('description_auto'),
        'missing':[k for k in ['display_name','role','responsibilities'] if not data.get(k)]
    })
print(json.dumps({'count':len(profiles),'missing':[p for p in profiles if p['missing']]}, ensure_ascii=False, indent=2))
PY
```

结果摘要：

```json
{
  "count": 26,
  "missing": []
}
```

### 2. CLI 可见入口

命令：

```bash
hermes profile list
```

结果摘要：

```text
Profile            员工 / 岗位                         Model      Gateway
frontend-eng       冯前端 / Web 前端工程师              gpt-5.5    running
pm                 沈产品 / 产品总监 / 项目经理          gpt-5.5    running
qa-eng             秦质检 / QA 工程师                   gpt-5.5    running
ux-designer        游体验 / UX 设计师                   gpt-5.5    running
```

完整输出已在本任务运行日志中记录；检查重点是 `员工 / 岗位` 列存在，且中文姓名、岗位可直接识别。

### 3. Web API 元数据读取

命令：

```bash
python - <<'PY'
from hermes_cli import profiles
items=profiles.list_profiles()
print('profiles_from_api_source', len(items))
print('missing_required_metadata', [p.name for p in items if not getattr(p,'display_name','') or not getattr(p,'role','') or not (getattr(p,'responsibilities',[]) or [])])
print('first_three', [(p.name, getattr(p,'display_name',''), getattr(p,'role',''), len(getattr(p,'responsibilities',[]) or [])) for p in items[:3]])
PY
```

结果：

```text
profiles_from_api_source 26
missing_required_metadata []
first_three [('default', '赫尔墨斯', '总控助手 / 董事会秘书', 4), ('api-contract-eng', '齐契约', 'API 契约工程师', 4), ('architect', '顾架构', '架构师', 4)]
```

### 4. Web UI 构建

目录：`C:/Users/q4528/AppData/Local/hermes/hermes-agent/web`

命令：

```bash
npm run build
```

结果：

```text
✓ 2077 modules transformed.
✓ built in 5.45s
```

说明：Vite 仍提示部分 chunk 大于 500 kB，这是既有前端打包体积提示，不影响本次员工元数据展示路径验收。

### 5. Diff 清洁检查

PrivateCloudDrive 证据文档所在 checkout：

```bash
git diff --check
```

Hermes Agent 展示变更：

```bash
git -C C:/Users/q4528/AppData/Local/hermes/hermes-agent diff --check -- hermes_cli/main.py hermes_cli/profiles.py hermes_cli/web_server.py web/src/lib/api.ts web/src/pages/ProfilesPage.tsx
```

结果：PASS；Hermes Agent 仓库仅出现 Windows CRLF 提示，无 whitespace error。

## 变更/证据文件

- `docs/validation/kanban-t_4ab7e3d6-web-roster-entry-check.md`：本验证记录
- `docs/validation/kanban-t_4ab7e3d6-hermes-profile-roster-ui.diff`：Hermes Agent 项目外 UI/API/CLI 展示变更 diff 证据

## 风险与边界

- 本任务没有公开发布，也没有执行破坏性数据操作。
- Hermes Agent 源码目录位于 PrivateCloudDrive 项目外：`C:/Users/q4528/AppData/Local/hermes/hermes-agent`。相关 UI/API/CLI 文件仍是该仓库未提交改动，本任务仅在 PrivateCloudDrive 中保存验证证据。
- 当前原始工作区根目录的 `.git` 元数据不完整，无法直接在根目录执行 `git status`；为避免破坏现状，本轮使用 `_kanban_t_4ab7e3d6_checkout2` 作为干净验证 checkout。