# Vendor — CSZoneNet (internalisé le 2026-07-03)

Sources internalisées des deux packages NuGet abandonnés (dernières publications avril 2024) :

- `CSZoneNet.Plugin.CS2BaseAllocator 1.0.13` — source : https://github.com/LordFetznschaedl/CS2BaseAllocator (branche `main`)
- `CSZoneNet.Plugin.Utils 1.0.1` — source : https://github.com/LordFetznschaedl/CSZonePluginUtils (branche `main`)

Licence d'origine : GPL (voir les fichiers LICENSE des dépôts source). Auteur : LordFetznschaedl.

La surface publique des DLL NuGet a été comparée par réflexion aux sources GitHub avant
internalisation : identique (types, membres, valeurs d'enums).

**Pourquoi** : dépendances abandonnées, compilées pour net8/ancienne API CSSharp — les compiler
dans le plugin supprime le risque de rupture binaire à chaque montée de version CounterStrikeSharp
(constat de l'audit `docs/AUDIT-2026-07-03.md`).

Les namespaces d'origine (`CSZoneNet.Plugin.*`) sont conservés pour ne pas toucher au reste du
code, et le format du JSON `configs/allocators/BaseAllocator/GrenadeKits.json` reste inchangé.
`#nullable disable` est appliqué à ces fichiers pour préserver la sémantique d'origine
(les libs étaient compilées sans nullable).
