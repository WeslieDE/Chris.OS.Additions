This module for OpenSimulator offers some extra functions for OpenSimulator. Some new script functions are provided and more modules for the region. So it is possible to access a K/V database from scripts, region wide object search without sensor and osGetInventoryList() to read the whole inventory of an object.

Then there are some more functions for the region. Region Auto Restart, Warp3D Image Cache, JPEG Image Converter, Region Idel Module and TexturFetcher.

Some new services are also offered by Robust. There is an AssetProxyClient and a new HG friends service.

## OpenSim.ini

    [RegionIdelMode]
        Enabled = false
        Scripts = true
        Physics = true
        SoftMode = true
    
    [FullPerm]
        Enabled = false
    
    [CachedRegionImageModule]
        MapImageModule = CachedRegionImageModule
        CacheDirectory = ./../MapImageCache
        GenerateMaptiles = true
        enableDate = false
        enableName = false
        enablePosition = false
        RefreshEveryMonth = true
        enableHostedBy = false
        HosterText = ""
    
    [TextureFetcher]
        Enable = false
        TextureFetcherCheckAssets = false
    
    [AutoRestart]
        Enable = false

    [ScriptDataStorage]
        DataStorageTyp = Memory

## Robust.ini

    HGFriendsServerConnector = "${Const|PublicPort}/Chris.OS.Additions.dll:FriendServerConnector"

    AssetServiceConnector = "${Const|PrivatePort}/Chris.OS.Additions.dll:AssetProxyConnector"
    HGAssetServiceConnector = "${Const|PublicPort}/Chris.OS.Additions.dll:AssetProxyConnector"
