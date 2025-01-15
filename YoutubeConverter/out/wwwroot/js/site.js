function startConversion() {
    const videoUrl = $('#videoUrl').val();
    if (!videoUrl) {
        alert('Por favor, insira um link válido.');
        return;
    }

    $('#progress-bar').css('width', '0%');
    $('#progress-text').text('0%');
    $('#progress-container').css('display', 'none'); 

    $.post('/Youtube/Convert', { VideoUrl: videoUrl }, function (response) {
        window.location.href = '/Youtube/Download';
    });

    const interval = setInterval(function () {
        $.get('/Youtube/Progress', function (data) {
            const progress = data.progress;

            if (progress !== '0%') {
                $('#progress-container').css('display', 'block');
                $('#progress-text').css('display', 'block');
            }

            $('#progress-bar').css('width', progress);
            $('#progress-text').text(progress);

            if (progress === '100%') {
                clearInterval(interval);
            }
        });
    }, 1000); // Consulta o progresso a cada 1 segundo
}
