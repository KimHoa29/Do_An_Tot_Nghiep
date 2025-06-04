const DocumentManager = {
    init() {
        // Initialize like buttons
        document.querySelectorAll('.like-btn').forEach(btn => {
            const documentId = btn.dataset.documentId;
            const isLiked = btn.dataset.liked === 'true';
            const countSpan = document.getElementById(`like-count-${documentId}`);
            if (countSpan) {
                countSpan.textContent = countSpan.textContent || '0';
            }
            if (isLiked) {
                btn.classList.add('liked');
            }
        });
    },

    toggleLike(documentId) {
        const likeBtn = document.querySelector(`.like-btn[data-document-id="${documentId}"]`);
        const likeCountSpan = document.getElementById(`like-count-${documentId}`);
        if (!likeBtn || !likeCountSpan) return;
        fetch(`/Documents/ToggleLike/${documentId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            credentials: 'same-origin'
        })
        .then(response => response.json())
        .then(data => {
            if (data.needLogin) {
                window.location.href = '/Account/Login';
                return;
            }
            if (data.success) {
                likeBtn.dataset.liked = data.isLiked.toString();
                if (data.isLiked) {
                    likeBtn.classList.add('liked');
                } else {
                    likeBtn.classList.remove('liked');
                }
                likeCountSpan.textContent = data.likeCount;
            } else {
                alert(data.message || 'Có lỗi xảy ra khi thực hiện thao tác');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            alert('Có lỗi xảy ra khi thực hiện thao tác. Vui lòng thử lại sau.');
        });
    }
};

document.addEventListener('DOMContentLoaded', () => {
    DocumentManager.init();
}); 