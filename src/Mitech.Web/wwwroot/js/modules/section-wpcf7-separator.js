/**
 * .section 要素の中に .wpcf7（Contact Form 7）が存在する場合に、
 * DOM構造を分割して .wpcf7 を .section の外に移動するためのスクリプト。
 *
 * 【目的】
 * - Contact Form 7 のHTML構造を .section の影響（余白・背景・レイアウト等）から切り離す
 * - デザイン崩れや意図しないスタイル継承を防ぐ
 *
 * 【処理内容】
 * 1. すべての .section 要素を取得
 * 2. 各 .section の「直下」に .wpcf7 が存在するかを確認
 * 3. 存在する場合：
 *    - .wpcf7 の直前で .section を終了
 *    - .wpcf7 自体を .section の外へ移動
 *    - .wpcf7 より後ろの要素を、新しい .section として再ラップ
 */
document.addEventListener('DOMContentLoaded', () => {
    const sections = document.querySelectorAll('.section');

    sections.forEach((section) => {
        // section直下のwpcf7のみ対象
        const wpcf7 = Array.from(section.children).find(
            (child) => child.classList.contains('wpcf7')
        );

        if (!wpcf7) return;

        const parent = section.parentNode;

        // wpcf7以降の要素を新しいsectionに移す
        const newSection = document.createElement('div');
        newSection.className = 'section';

        let next = wpcf7.nextElementSibling;
        while (next) {
            const current = next;
            next = next.nextElementSibling;
            newSection.appendChild(current);
        }

        // section → wpcf7 → newSection の順に並べる
        parent.insertBefore(wpcf7, section.nextSibling);

        if (newSection.children.length > 0) {
            parent.insertBefore(newSection, wpcf7.nextSibling);
        }
    });
});
