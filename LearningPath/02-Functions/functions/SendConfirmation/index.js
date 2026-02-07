module.exports = async function (context, req) {
  const body = req.body || {};

  context.log('SendConfirmation called', {
    sessionId: body.sessionId,
    attendeeEmail: body.attendeeEmail
  });

  return {
    status: 200,
    headers: { 'Content-Type': 'application/json' },
    body: {
      ok: true,
      message: 'Confirmation accepted by Azure Function',
      attendeeEmail: body.attendeeEmail || null,
      sessionTitle: body.sessionTitle || null
    }
  };
};
