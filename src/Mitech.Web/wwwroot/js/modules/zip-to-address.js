/**
 * 住所自動入力スクリプト
 */
(function () {
    'use strict';

    const init = () => {
        const zip = document.getElementById('zip');
        if (!zip || typeof window.AjaxZip3 === 'undefined') {
            return;
        }

        const handler = () => {
            // 住所フィールド名
            window.AjaxZip3.zip2addr(zip, '', 'address', 'address');
        };

        zip.addEventListener('input', () => {
            zip.value = zip.value.replace(/[^\d]/g, '');
        });

        zip.addEventListener('keyup', handler);
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    document.addEventListener('wpcf7init', init);
})();