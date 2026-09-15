/**
 * 指定されたname属性を持つinput要素にinputmodeを設定する
 */
(function() {
    'use strict';

    // 以下に追加したいname属性とinputmodeを記述
    const inputmodeConfig = {
        'zip': 'numeric',
        // 例: 'tel': 'tel',
        // 例: 'email': 'email',
    };

    function setInputMode() {
        try {
            Object.keys(inputmodeConfig).forEach(function(nameAttr) {
                const inputmodeValue = inputmodeConfig[nameAttr];

                const inputs = document.querySelectorAll('input[name="' + nameAttr + '"]');

                inputs.forEach(function(input) {
                    input.setAttribute('inputmode', inputmodeValue);
                });
            });
        } catch (error) {
            console.error('inputmode設定中にエラーが発生しました:', error);
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', setInputMode);
    } else {
        setInputMode();
    }
})();