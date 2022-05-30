﻿export async function  syncDatabaseWithBrowserCache(filename) {   
    window.blazorWasmDatabase = window.blazorWasmDatabase || {
        init: false,
        cache: await caches.open('blazorWasmDatabase')
    };
    
    const backupPath = `/backup/database/${filename}`;
    const cachePath = `/cache/database/${filename.substring(0, filename.indexOf('_backup'))}`;
    
    if (!window.blazorWasmDatabase.init) {
        window.blazorWasmDatabase.init = true;
        
        const cacheResponse = await window.blazorWasmDatabase.cache.match(cachePath);
        if(cacheResponse && cacheResponse.ok) {
            const cacheResponseArrayBuffer = await cacheResponse.arrayBuffer();
            if(cacheResponseArrayBuffer){
                console.log(`Restoring ${cacheResponseArrayBuffer.byteLength} bytes`);
                FS.writeFile(backupPath, new Uint8Array(cacheResponseArrayBuffer));
                return 0;
            }
        }
        return  -1;
    }

    if (FS.analyzePath(backupPath).exists) {

        const waitFlush = new Promise((done, _) => {
            setTimeout(done, 10);
        });

        await waitFlush;

        const data = FS.readFile(backupPath);

        const blob = new Blob([data], {
            type: 'application/octet-stream',
            ok: true,
            status: 200
        });

        const headers = new Headers({
            'content-length': blob.size
        });

        const response = new Response(blob, {
            headers
        });

        await window.blazorWasmDatabase.cache.put(cachePath, response);

        FS.unlink(backupPath);

        return 1;
    }
    return -1;    
}

export async function generateDownloadLinkAsync(filename) {    
    const cachePath = `/cache/database/${filename.substring(0, filename.indexOf('_backup'))}`;    
    const cacheResponse = await window.blazorWasmDatabase.cache.match(cachePath);

    if (cacheResponse && cacheResponse.ok) {
        const blobResponse = await cacheResponse.blob();
        if (blobResponse) { return URL.createObjectURL(blobResponse); }
    }
    return '';
}